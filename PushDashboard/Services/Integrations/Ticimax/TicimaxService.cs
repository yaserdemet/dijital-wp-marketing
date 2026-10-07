using System.ServiceModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PushDashboard.CustomServis;
using PushDashboard.SiparisServis;
using PushDashboard.UyeServis;
using PushDashboard.UrunServis;
using PushDashboard.Data;
using PushDashboard.Services.Integrations.Common;
using PushDashboard.Services.Integrations.Common.Models;
using PushDashboard.Services.Integrations.Ticimax.Mappers;

namespace PushDashboard.Services.Integrations.Ticimax;

/// <summary>
/// Ticimax API ile iletişim kuran temel servis
/// Implements global IEcommerceService interface for platform-agnostic operations
/// </summary>
public class TicimaxService : IEcommerceService
{
    private readonly ILogger<TicimaxService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    // Cache key prefixes
    private const string TedarikciCacheKeyPrefix = "Ticimax_Tedarikci_";
    private const string EtiketCacheKeyPrefix = "Ticimax_Etiket_";
    private const string ProductsCacheKeyPrefix = "Ticimax_Products_";
    private const string ProductCacheKeyPrefix = "Ticimax_Product_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30); // Tüm cache'ler için 30 dakika

    // Aktif cache key'lerini tutmak için (invalidation için)
    private static readonly HashSet<string> _activeProductCacheKeys = new();

    public TicimaxService(ILogger<TicimaxService> logger, ApplicationDbContext context, IMemoryCache cache)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// Platform identifier for Ticimax
    /// </summary>
    public string PlatformName => "Ticimax";

    #region Client Factory Methods

    /// <summary>
    /// CustomServis SOAP client'ı oluşturur
    /// </summary>
    public CustomServisClient CreateCustomServiceClient(string baseUrl)
    {
        var serviceUrl = $"{baseUrl.TrimEnd('/')}/Servis/CustomServis.svc";
        var binding = new BasicHttpBinding();
        binding.MaxBufferSize = int.MaxValue;
        binding.MaxReceivedMessageSize = int.MaxValue;
        binding.MaxBufferPoolSize = int.MaxValue;
        binding.Security.Mode = BasicHttpSecurityMode.Transport;
        var endpoint = new EndpointAddress(serviceUrl);
        return new CustomServisClient(binding, endpoint);
    }

    /// <summary>
    /// SiparisServis SOAP client'ı oluşturur
    /// </summary>
    public SiparisServisClient CreateOrderServiceClient(string baseUrl)
    {
        var serviceUrl = $"{baseUrl.TrimEnd('/')}/Servis/SiparisServis.svc";
        var binding = new BasicHttpBinding();
        binding.MaxBufferSize = int.MaxValue;
        binding.MaxReceivedMessageSize = int.MaxValue;
        binding.MaxBufferPoolSize = int.MaxValue;
        binding.Security.Mode = BasicHttpSecurityMode.Transport;
        var endpoint = new EndpointAddress(serviceUrl);
        return new SiparisServisClient(binding, endpoint);
    }

    /// <summary>
    /// UyeServis SOAP client'ı oluşturur
    /// </summary>
    public UyeServisClient CreateMemberServiceClient(string baseUrl)
    {
        var serviceUrl = $"{baseUrl.TrimEnd('/')}/Servis/UyeServis.svc";
        var binding = new BasicHttpBinding();
        binding.MaxBufferSize = int.MaxValue;
        binding.MaxReceivedMessageSize = int.MaxValue;
        binding.MaxBufferPoolSize = int.MaxValue;
        binding.Security.Mode = BasicHttpSecurityMode.Transport;
        var endpoint = new EndpointAddress(serviceUrl);
        return new UyeServisClient(binding, endpoint);
    }

    /// <summary>
    /// UrunServis SOAP client'ı oluşturur
    /// </summary>
    public UrunServisClient CreateProductServiceClient(string baseUrl)
    {
        var serviceUrl = $"{baseUrl.TrimEnd('/')}/Servis/UrunServis.svc";
        var binding = new BasicHttpBinding();
        binding.MaxBufferSize = int.MaxValue;
        binding.MaxReceivedMessageSize = int.MaxValue;
        binding.MaxBufferPoolSize = int.MaxValue;
        binding.SendTimeout = TimeSpan.FromMinutes(10);
        binding.Security.Mode = BasicHttpSecurityMode.Transport;
        var endpoint = new EndpointAddress(serviceUrl);
        return new UrunServisClient(binding, endpoint);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Company ID'den Ticimax API bilgilerini getirir
    /// </summary>
    private async Task<(string ApiKey, string BaseUrl)?> GetTicimaxApiInfoAsync(Guid companyId)
    {
        try
        {
            var integration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "Ticimax" &&
                                         ci.IsActive && ci.IsConfigured);

            if (integration?.Settings == null)
            {
                _logger.LogWarning("Ticimax integration not found or not configured for company {CompanyId}", companyId);
                return null;
            }

            var apiKey = integration.Settings.GetValueOrDefault("apiKey")?.ToString();
            var baseUrl = integration.Settings.GetValueOrDefault("apiUrl")?.ToString();

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("Ticimax API credentials incomplete for company {CompanyId}", companyId);
                return null;
            }

            return (apiKey, baseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Ticimax API info for company {CompanyId}", companyId);
            return null;
        }
    }

    #endregion

    #region IEcommerceService Implementation

    /// <summary>
    /// Gets webhooks using global e-commerce models
    /// </summary>
    public async Task<EcommerceWebhook[]> GetWebhooksAsync(Guid companyId)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return new EcommerceWebhook[0];

        try
        {
            using var client = CreateCustomServiceClient(apiInfo.Value.BaseUrl);

            var request = new SelectWebhookRequest
            {
                ID = 0, // Tüm webhook'ları getir
                IslemTipi = null // Tüm işlem tiplerini getir
            };

            var response = await client.SelectWebhookAsync(apiInfo.Value.ApiKey, request);

            if (response != null && !response.IsError)
            {
                var ticimaxWebhooks = response.WebhookList ?? new Webhook[0];
                return ticimaxWebhooks.Select(TicimaxMapper.MapToEcommerceWebhook).ToArray();
            }

            _logger.LogWarning("Ticimax webhook query returned error: {Error}", response?.ErrorMessage);
            return new EcommerceWebhook[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhooks from Ticimax API for company {CompanyId}", companyId);
            return new EcommerceWebhook[0];
        }
    }

    /// <summary>
    /// Saves webhook using global e-commerce models
    /// </summary>
    public async Task<EcommerceWebhookResult> SaveWebhookAsync(Guid companyId, EcommerceWebhookRequest request)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return new EcommerceWebhookResult
            {
                Success = false,
                Message = "Ticimax entegrasyonu bulunamadı veya yapılandırılmamış",
                ErrorCode = "INTEGRATION_NOT_FOUND"
            };

        try
        {
            using var client = CreateCustomServiceClient(apiInfo.Value.BaseUrl);

            var ticimaxWebhook = TicimaxMapper.MapToTicimaxWebhook(request);
            var ticimaxEventType = TicimaxMapper.MapToTicimaxWebhookType(request.EventType);

            var webhook = new Webhook
            {
                ID = 0, // Yeni webhook için 0
                IslemTipi = ticimaxEventType,
                Url = request.Url,
                KullaniciAdi = request.Username ?? "pushonica",
                Sifre = request.Password ?? "pushonica123!"
            };

            var response = await client.SaveWebhookAsync(apiInfo.Value.ApiKey, webhook);

            if (response != null && !response.IsError)
            {
                _logger.LogInformation("Webhook saved successfully for company {CompanyId}: {EventType} -> {Url}",
                    companyId, request.EventType, request.Url);

                return new EcommerceWebhookResult
                {
                    Success = true,
                    Message = "Webhook başarıyla kaydedildi",
                    WebhookId = response.ID.ToString(),
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["platform"] = PlatformName,
                        ["ticimaxWebhookId"] = response.ID
                    }
                };
            }

            var errorMessage = response?.ErrorMessage ?? "Bilinmeyen hata";
            _logger.LogWarning("Failed to save webhook for company {CompanyId}: {Error}", companyId, errorMessage);

            return new EcommerceWebhookResult
            {
                Success = false,
                Message = errorMessage,
                ErrorCode = "TICIMAX_ERROR"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving webhook for company {CompanyId}", companyId);
            return new EcommerceWebhookResult
            {
                Success = false,
                Message = "Webhook kaydedilirken hata oluştu: " + ex.Message,
                ErrorCode = "EXCEPTION"
            };
        }
    }

    /// <summary>
    /// Deletes webhook using global e-commerce models
    /// </summary>
    public async Task<EcommerceResult> DeleteWebhookAsync(Guid companyId, string webhookId)
    {
        if (!int.TryParse(webhookId, out var ticimaxWebhookId))
        {
            return new EcommerceResult
            {
                Success = false,
                Message = "Invalid webhook ID format",
                ErrorCode = "INVALID_WEBHOOK_ID"
            };
        }

        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return new EcommerceResult
            {
                Success = false,
                Message = "Ticimax entegrasyonu bulunamadı veya yapılandırılmamış",
                ErrorCode = "INTEGRATION_NOT_FOUND"
            };

        try
        {
            using var client = CreateCustomServiceClient(apiInfo.Value.BaseUrl);

            var response = await client.DeleteWebhookAsync(apiInfo.Value.ApiKey, ticimaxWebhookId);

            if (response != null && !response.IsError)
            {
                _logger.LogInformation("Webhook deleted successfully for company {CompanyId}: {WebhookId}", companyId, ticimaxWebhookId);
                return new EcommerceResult
                {
                    Success = true,
                    Message = "Webhook başarıyla silindi",
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["platform"] = PlatformName,
                        ["ticimaxWebhookId"] = ticimaxWebhookId
                    }
                };
            }

            var errorMessage = response?.ErrorMessage ?? "Bilinmeyen hata";
            _logger.LogWarning("Failed to delete webhook for company {CompanyId}: {Error}", companyId, errorMessage);
            return new EcommerceResult
            {
                Success = false,
                Message = errorMessage,
                ErrorCode = "TICIMAX_ERROR"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook for company {CompanyId}", companyId);
            return new EcommerceResult
            {
                Success = false,
                Message = "Webhook silinirken hata oluştu: " + ex.Message,
                ErrorCode = "EXCEPTION"
            };
        }
    }

    /// <summary>
    /// Gets customers using global e-commerce models
    /// Fetches all pages until a page returns less than PageSize (50) records
    /// </summary>
    public async Task<EcommerceCustomer[]> GetCustomersAsync(Guid companyId, EcommerceCustomerFilter filter, EcommercePagination pagination)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
        {
            _logger.LogWarning("GetCustomersAsync: API info is null for company {CompanyId}", companyId);
            return new EcommerceCustomer[0];
        }

        _logger.LogInformation("GetCustomersAsync: Fetching all customers from Ticimax API. BaseUrl: {BaseUrl}, CompanyId: {CompanyId}",
            apiInfo.Value.BaseUrl, companyId);

        try
        {
            var allCustomers = new List<EcommerceCustomer>();
            var ticimaxFilter = TicimaxMapper.MapToTicimaxCustomerFilter(filter);
            var pageNumber = 1;
            var pageSize = pagination.PageSize > 0 ? pagination.PageSize : 50;

            using var client = CreateMemberServiceClient(apiInfo.Value.BaseUrl);

            while (true)
            {
                var ticimaxPagination = new UyeSayfalama
                {
                    SayfaNo = pageNumber,
                    KayitSayisi = pageSize
                };

                _logger.LogInformation("GetCustomersAsync: Fetching page {PageNumber} (PageSize: {PageSize})",
                    pageNumber, pageSize);

                var ticimaxCustomers = await client.SelectUyelerAsync(apiInfo.Value.ApiKey, ticimaxFilter, ticimaxPagination);
                var fetchedCount = ticimaxCustomers?.Length ?? 0;

                _logger.LogInformation("GetCustomersAsync: Page {PageNumber} returned {Count} customers", pageNumber, fetchedCount);

                if (ticimaxCustomers != null && ticimaxCustomers.Length > 0)
                {
                    var mappedCustomers = ticimaxCustomers.Select(TicimaxMapper.MapToEcommerceCustomer);
                    allCustomers.AddRange(mappedCustomers);
                }

                // Eğer gelen kayıt sayısı pageSize'dan küçükse, son sayfaya ulaştık demektir
                if (fetchedCount < pageSize)
                {
                    _logger.LogInformation("GetCustomersAsync: Last page reached. Total customers fetched: {TotalCount}", allCustomers.Count);
                    break;
                }

                pageNumber++;
            }

            _logger.LogInformation("GetCustomersAsync: Completed. Total {Count} customers fetched from Ticimax API for company {CompanyId}",
                allCustomers.Count, companyId);

            return allCustomers.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers from Ticimax API for company {CompanyId}. Error: {ErrorMessage}",
                companyId, ex.Message);
            return new EcommerceCustomer[0];
        }
    }

    /// <summary>
    /// Gets a specific customer by external ID
    /// </summary>
    public async Task<EcommerceCustomer?> GetCustomerAsync(Guid companyId, string externalCustomerId)
    {
        if (!int.TryParse(externalCustomerId, out var ticimaxCustomerId))
        {
            return null;
        }

        var filter = new EcommerceCustomerFilter
        {
            MemberId = externalCustomerId
        };
        var pagination = new EcommercePagination
        {
            PageNumber = 1,
            PageSize = 100, // Get more records to search through
            SortField = "ID",
            SortDirection = "ASC"
        };

        // Note: Ticimax doesn't have a direct "get customer by ID" method
        // We get customers and filter client-side (not ideal for production)
        var customers = await GetCustomersAsync(companyId, filter, pagination);
        return customers.FirstOrDefault(c => c.ExternalId == externalCustomerId);
    }

    /// <summary>
    /// Gets orders using global e-commerce models
    /// </summary>
    public async Task<EcommerceOrder[]> GetOrdersAsync(Guid companyId, EcommerceOrderFilter filter, EcommercePagination pagination)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return new EcommerceOrder[0];

        try
        {
            var ticimaxFilter = TicimaxMapper.MapToTicimaxOrderFilter(filter);
            var ticimaxPagination = TicimaxMapper.MapToTicimaxOrderPagination(pagination);

            using var client = CreateOrderServiceClient(apiInfo.Value.BaseUrl);
            var ticimaxOrders = await client.SelectSiparisAsync(apiInfo.Value.ApiKey, ticimaxFilter, ticimaxPagination);

            return ticimaxOrders?.Select(TicimaxMapper.MapToEcommerceOrder).ToArray() ?? new EcommerceOrder[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders from Ticimax API for company {CompanyId}", companyId);
            return new EcommerceOrder[0];
        }
    }

    /// <summary>
    /// Gets a specific order by external ID
    /// </summary>
    public async Task<EcommerceOrder?> GetOrderAsync(Guid companyId, string externalOrderId)
    {
        if (!int.TryParse(externalOrderId, out var ticimaxOrderId))
        {
            return null;
        }

        var filter = new EcommerceOrderFilter()
        {
            OrderId = ticimaxOrderId
        };
        var pagination = new EcommercePagination
        {
            PageNumber = 1,
            PageSize = 100, // Get more records to search through
            SortField = "ID",
            SortDirection = "ASC"
        };

        // Note: Similar to customers, Ticimax doesn't have direct "get order by ID"
        // This is a simplified implementation
        var orders = await GetOrdersAsync(companyId, filter, pagination);
        return orders.FirstOrDefault(o => o.ExternalId == externalOrderId);
    }

    /// <summary>
    /// Gets carts using global e-commerce models
    /// </summary>
    public async Task<EcommerceCart[]> GetCartsAsync(Guid companyId, EcommerceCartFilter filter, EcommercePagination pagination)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return new EcommerceCart[0];

        try
        {
            using var client = CreateOrderServiceClient(apiInfo.Value.BaseUrl);
            var response = await client.SelectSepetAsync(apiInfo.Value.ApiKey, 0, 0, filter.CreatedAfter, filter.CreatedBefore, pagination.PageNumber, null);

            return response?.Sepetler?.Select(TicimaxMapper.MapToEcommerceCart).ToArray() ?? new EcommerceCart[0];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting carts from Ticimax API for company {CompanyId}", companyId);
            return new EcommerceCart[0];
        }
    }

    #region Product Operations

    /// <summary>
    /// Gets products using global e-commerce models from Ticimax UrunServis
    /// </summary>
    public async Task<EcommerceProduct[]> GetProductsAsync(Guid companyId, EcommerceProductFilter filter, EcommercePagination pagination)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return new EcommerceProduct[0];

        try
        {
            using var client = CreateProductServiceClient(apiInfo.Value.BaseUrl);

            // UrunFiltre oluştur
            var urunFiltre = new UrunFiltre
            {
                Aktif = filter.IsActive.HasValue ? (filter.IsActive.Value ? 1 : 0) : -1, // -1 = tümü, 0 = pasif, 1 = aktif
            };

            // Arama terimi varsa stok koduna göre filtrele (UrunFiltre'de UrunAdi yok)
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                urunFiltre.StokKodu = filter.SearchTerm;
            }

            // Kategori filtresi
            if (!string.IsNullOrEmpty(filter.CategoryId) && int.TryParse(filter.CategoryId, out int kategoriId))
            {
                urunFiltre.KategoriID = kategoriId;
            }

            // Marka filtresi
            if (!string.IsNullOrEmpty(filter.BrandId) && int.TryParse(filter.BrandId, out int markaId))
            {
                urunFiltre.MarkaID = markaId;
            }

            // Tedarikçi filtresi
            if (filter.CustomFilters != null && filter.CustomFilters.TryGetValue("supplierId", out var supplierIdObj))
            {
                if (int.TryParse(supplierIdObj?.ToString(), out int tedarikciId))
                {
                    urunFiltre.TedarikciID = tedarikciId;
                }
            }

            // Sayfalama oluştur
            var sayfalama = new UrunSayfalama
            {
                BaslangicIndex = (pagination.PageNumber - 1) * pagination.PageSize,
                KayitSayisi = pagination.PageSize
            };

            var urunler = await client.SelectUrunAsync(apiInfo.Value.ApiKey, urunFiltre, sayfalama);

            if (urunler == null || urunler.Length == 0)
                return new EcommerceProduct[0];

            // Tedarikçi ve etiket isimlerini çek
            var tedarikciDict = await GetTedarikciDictionaryAsync(client, apiInfo.Value.ApiKey);
            var etiketDict = await GetEtiketDictionaryAsync(client, apiInfo.Value.ApiKey);

            // UrunKarti'ları EcommerceProduct'a dönüştür
            var products = urunler.Select(u => MapUrunKartiToEcommerceProduct(u, apiInfo.Value.BaseUrl, tedarikciDict, etiketDict)).ToList();

            // Arama terimi varsa ürün adına göre client-side filtrele (API'de UrunAdi filtresi yok)
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                products = products.Where(p =>
                    p.Name.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    p.ProductCode.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Fiyat filtreleri (API'de yoksa client-side uygula)
            if (filter.MinPrice.HasValue)
            {
                products = products.Where(p => p.Price >= filter.MinPrice.Value).ToList();
            }

            if (filter.MaxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= filter.MaxPrice.Value).ToList();
            }

            // Stok filtresi (API'de yoksa client-side uygula)
            if (filter.InStock.HasValue)
            {
                products = products.Where(p => p.InStock == filter.InStock.Value).ToList();
            }

            return products.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products from Ticimax UrunServis for company {CompanyId}", companyId);
            return new EcommerceProduct[0];
        }
    }

    /// <summary>
    /// Gets a specific product by external ID from Ticimax UrunServis
    /// </summary>
    public async Task<EcommerceProduct?> GetProductAsync(Guid companyId, string externalProductId)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return null;

        try
        {
            if (!int.TryParse(externalProductId, out int urunId))
                return null;

            using var client = CreateProductServiceClient(apiInfo.Value.BaseUrl);

            var urunFiltre = new UrunFiltre
            {
                UrunKartiID = urunId,
                Aktif = -1, // Tüm durumlar
                MarkaID = -1,
                TedarikciID = -1,
                Vitrin = -1,
                KasaOnuFirsatlari = -1,
                Indirimli = -1,
                KategoriID = -1,
                Firsat = -1
            };

            var sayfalama = new UrunSayfalama
            {
                BaslangicIndex = 0,
                KayitSayisi = 1
            };

            var urunler = await client.SelectUrunAsync(apiInfo.Value.ApiKey, urunFiltre, sayfalama);

            if (urunler == null || urunler.Length == 0)
                return null;
            var tedarikciDict = await GetTedarikciDictionaryAsync(client, apiInfo.Value.ApiKey);
            var etiketDict = await GetEtiketDictionaryAsync(client, apiInfo.Value.ApiKey);

            var product = MapUrunKartiToEcommerceProduct(urunler[0], apiInfo.Value.BaseUrl, tedarikciDict, etiketDict);

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId} from Ticimax UrunServis for company {CompanyId}", externalProductId, companyId);
            return null;
        }
    }
    
    /// <summary>
    /// Gets a specific product variant by external ID from Ticimax UrunServis
    /// </summary>
    public async Task<EcommerceProductVariant?> GetVariationAsync(Guid companyId, string externalProductId)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return null;

        try
        {
            if (!int.TryParse(externalProductId, out int urunId))
                return null;

            using var client = CreateProductServiceClient(apiInfo.Value.BaseUrl);

            var urunFiltre = new VaryasyonFiltre()
            {
                UrunID = urunId,
                Aktif = -1 // Tüm durumlar
            };

            var sayfalama = new UrunSayfalama
            {
                BaslangicIndex = 0,
                KayitSayisi = 1
            };

            var urunler = await client.SelectVaryasyonAsync(apiInfo.Value.ApiKey, urunFiltre, sayfalama, new SelectVaryasyonAyar(){});

            if (urunler == null || urunler.Length == 0)
                return null;

            var variant = MapVaryasyonToEcommerceProductVariant(urunler[0]);

            return variant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId} from Ticimax UrunServis for company {CompanyId}", externalProductId, companyId);
            return null;
        }
    }

    /// <summary>
    /// Gets total product count from Ticimax UrunServis
    /// </summary>
    public async Task<int> GetProductCountAsync(Guid companyId, EcommerceProductFilter filter)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return 0;

        try
        {
            using var client = CreateProductServiceClient(apiInfo.Value.BaseUrl);

            var urunFiltre = new UrunFiltre
            {
                Aktif = filter.IsActive.HasValue ? (filter.IsActive.Value ? 1 : 0) : -1,
            };

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                urunFiltre.StokKodu = filter.SearchTerm;
            }

            if (!string.IsNullOrEmpty(filter.CategoryId) && int.TryParse(filter.CategoryId, out int kategoriId))
            {
                urunFiltre.KategoriID = kategoriId;
            }

            if (!string.IsNullOrEmpty(filter.BrandId) && int.TryParse(filter.BrandId, out int markaId))
            {
                urunFiltre.MarkaID = markaId;
            }

            return await client.SelectUrunCountAsync(apiInfo.Value.ApiKey, urunFiltre);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product count from Ticimax UrunServis for company {CompanyId}", companyId);
            return 0;
        }
    }

    /// <summary>
    /// Maps Ticimax UrunKarti to EcommerceProduct
    /// </summary>
    private EcommerceProduct MapUrunKartiToEcommerceProduct(
        UrunKarti urun,
        string baseUrl,
        Dictionary<int, string>? tedarikciDict = null,
        Dictionary<int, string>? etiketDict = null)
    {
        // Etiketleri string listesine dönüştür (etiket isimlerini kullan)
        var tags = new List<string>();
        if (urun.Etiketler != null && urun.Etiketler.Length > 0)
        {
            if (etiketDict != null)
            {
                // Etiket dictionary varsa isimlerini al
                tags = urun.Etiketler
                    .Where(e => etiketDict.ContainsKey(e.EtiketID))
                    .Select(e => etiketDict[e.EtiketID])
                    .ToList();
            }
            else
            {
                // Dictionary yoksa ID'leri kullan (fallback)
                tags = urun.Etiketler.Select(e => e.EtiketID.ToString()).ToList();
            }
        }

        // Tedarikçi adını al
        var supplierName = urun.TedarikciKodu ?? string.Empty;
        if (tedarikciDict != null && urun.TedarikciID > 0 && tedarikciDict.ContainsKey(urun.TedarikciID))
        {
            supplierName = tedarikciDict[urun.TedarikciID];
        }

        // Resimleri listeye dönüştür
        var images = new List<string>();
        if (urun.Resimler != null && urun.Resimler.Length > 0)
        {
            images = urun.Resimler.ToList();
        }

        // Varyasyonları dönüştür
        var variants = new List<EcommerceProductVariant>();
        if (urun.Varyasyonlar != null && urun.Varyasyonlar.Length > 0)
        {
            variants = urun.Varyasyonlar.Select(v => MapVaryasyonToEcommerceProductVariant(v, urun.ID.ToString())).ToList();
        }

        // Varyasyonlardan fiyat ve stok bilgisi al (ilk varyasyon veya ana ürün)
        string stokKodu = null;
        decimal price = 0;
        decimal? discountPrice = null;
        string currency = "TRY";
        double taxRate = 18;
        bool kdvIncluded = true;
        int stockQuantity = (int)urun.ToplamStokAdedi;
        
        if (urun.Varyasyonlar != null && urun.Varyasyonlar.Length > 0)
        {
            var firstVariant = urun.Varyasyonlar[0];
            if (firstVariant.StokKodu != null) stokKodu = firstVariant.StokKodu;
            price = (decimal)firstVariant.SatisFiyati;
            currency = firstVariant.ParaBirimi;
            taxRate = (double)firstVariant.KdvOrani;
            kdvIncluded = firstVariant.KdvDahil;
            if (firstVariant.IndirimliFiyati > 0)
            {
                discountPrice = (decimal)firstVariant.IndirimliFiyati;
            }
        }

        return new EcommerceProduct
        {
            ExternalId = urun.ID.ToString(),
            ProductCode = urun.EntegrasyonID ?? urun.ID.ToString(),
            Name = urun.UrunAdi ?? string.Empty,
            Description = urun.Aciklama ?? string.Empty,
            ShortDescription = urun.OnYazi ?? string.Empty,
            Price = price,
            DiscountPrice = discountPrice,
            KDVAmount = null,
            TaxRate = taxRate,
            KdvIncluded = kdvIncluded,
            Currency = currency,
            CategoryId = urun.AnaKategoriID.ToString(),
            CategoryName = urun.AnaKategori ?? string.Empty,
            BrandId = urun.MarkaID.ToString(),
            BrandName = urun.Marka ?? string.Empty,
            SupplierId = urun.TedarikciID.ToString(),
            SupplierName = supplierName,
            Tags = tags,
            MainImage = images.FirstOrDefault() ?? string.Empty,
            Images = images,
            Variants = variants,
            StockQuantity = stockQuantity,
            InStock = stockQuantity > 0,
            IsActive = urun.Aktif,
            ProductUrl = !string.IsNullOrEmpty(urun.UrunSayfaAdresi)
                ? $"{baseUrl.TrimEnd('/')}/{urun.UrunSayfaAdresi.TrimStart('/')}"
                : string.Empty,
            CreatedAt = urun.EklemeTarihi,
            UpdatedAt = urun.YayinTarihi,
            CustomFields = new Dictionary<string, object>
            {
                ["ticimaxId"] = urun.ID,
                ["stokKodu"] = stokKodu,
                ["kategoriler"] = urun.Kategoriler ?? Array.Empty<int>(),
                ["vitrin"] = urun.Vitrin,
                ["yeniUrun"] = urun.YeniUrun,
                ["firsatUrunu"] = urun.FirsatUrunu,
                ["ucretsizKargo"] = urun.UcretsizKargo,
                ["seoBaslik"] = urun.SeoSayfaBaslik ?? string.Empty,
                ["seoAciklama"] = urun.SeoSayfaAciklama ?? string.Empty,
                ["varyasyonSayisi"] =urun.Varyasyonlar.Count()
            }
        };
    }

    /// <summary>
    /// Maps Ticimax Varyasyon to EcommerceProductVariant
    /// </summary>
    private EcommerceProductVariant MapVaryasyonToEcommerceProductVariant(Varyasyon varyasyon, string productId = "")
    {
        var variantName = string.Empty;
        if (varyasyon.Ozellikler != null && varyasyon.Ozellikler.Length > 0)
        {
            variantName = string.Join(" - ", varyasyon.Ozellikler.Select(o => o.Deger));
        }

        return new EcommerceProductVariant
        {
            ExternalId = varyasyon.UrunKartiID.ToString(),
            ProductId = productId,
            VariantCode = varyasyon.StokKodu ?? string.Empty,
            Name = variantName,
            Price = (decimal)varyasyon.SatisFiyati,
            DiscountPrice = varyasyon.IndirimliFiyati > 0 ? (decimal)varyasyon.IndirimliFiyati : null,
            StockQuantity = (int)varyasyon.StokAdedi,
            IsActive = varyasyon.Aktif,
            Barcode = varyasyon.Barkod,
            Currency = varyasyon.ParaBirimi ?? "TRY",
            CustomFields = new Dictionary<string, object>
            {
                ["ticimaxVaryasyonId"] = varyasyon.ID,
                ["kdvOrani"] = varyasyon.KdvOrani,
                ["kdvDahil"] = varyasyon.KdvDahil,
            }
        };
    }

    /// <summary>
    /// Tüm tedarikçileri çekip ID -> Tanim dictionary'si oluşturur (cache'li)
    /// </summary>
    private async Task<Dictionary<int, string>> GetTedarikciDictionaryAsync(UrunServisClient client, string apiKey)
    {
        var cacheKey = $"{TedarikciCacheKeyPrefix}{apiKey}";

        // Cache'den kontrol et
        if (_cache.TryGetValue(cacheKey, out Dictionary<int, string>? cachedDict) && cachedDict != null && cachedDict.Count > 0)
        {
            _logger.LogDebug("Tedarikçi listesi cache'den alındı. Boyut: {Count}", cachedDict.Count);
            return cachedDict;
        }

        var dict = new Dictionary<int, string>();
        try
        {
            // tedarikciID = 0, kategoriID = 0 ile tüm tedarikçileri çek
            _logger.LogDebug("Tedarikçi listesi API'den çekiliyor...");
            var tedarikciler = await client.SelectTedarikciAsync(apiKey, 0, 0);
            if (tedarikciler != null && tedarikciler.Length > 0)
            {
                _logger.LogDebug("Tedarikçi sayısı: {Count}", tedarikciler.Length);
                foreach (var t in tedarikciler)
                {
                    if (!string.IsNullOrEmpty(t.Tanim))
                    {
                        dict[t.ID] = t.Tanim;
                    }
                }

                // Cache'e kaydet (sadece veri varsa)
                if (dict.Count > 0)
                {
                    _cache.Set(cacheKey, dict, CacheDuration);
                    _logger.LogDebug("Tedarikçi listesi cache'e kaydedildi. Boyut: {Count}", dict.Count);
                }
            }
            else
            {
                _logger.LogWarning("Tedarikçi listesi null veya boş döndü");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tedarikçi listesi alınamadı, fallback olarak kod kullanılacak");
        }
        return dict;
    }

    /// <summary>
    /// Tüm etiketleri çekip ID -> Tanim dictionary'si oluşturur (cache'li)
    /// </summary>
    private async Task<Dictionary<int, string>> GetEtiketDictionaryAsync(UrunServisClient client, string apiKey)
    {
        var cacheKey = $"{EtiketCacheKeyPrefix}{apiKey}";

        // Cache'den kontrol et
        if (_cache.TryGetValue(cacheKey, out Dictionary<int, string>? cachedDict) && cachedDict != null && cachedDict.Count > 0)
        {
            _logger.LogDebug("Etiket listesi cache'den alındı. Boyut: {Count}", cachedDict.Count);
            return cachedDict;
        }

        var dict = new Dictionary<int, string>();
        try
        {
            // etiketID = 0 ile tüm etiketleri çek
            _logger.LogDebug("Etiket listesi API'den çekiliyor...");
            var etiketler = await client.SelectEtiketAsync(apiKey, 0);
            if (etiketler != null && etiketler.Length > 0)
            {
                _logger.LogDebug("Etiket sayısı: {Count}", etiketler.Length);
                foreach (var e in etiketler)
                {
                    if (!string.IsNullOrEmpty(e.Tanim))
                    {
                        dict[e.ID] = e.Tanim;
                    }
                }

                // Cache'e kaydet (sadece veri varsa)
                if (dict.Count > 0)
                {
                    _cache.Set(cacheKey, dict, CacheDuration);
                    _logger.LogDebug("Etiket listesi cache'e kaydedildi. Boyut: {Count}", dict.Count);
                }
            }
            else
            {
                _logger.LogWarning("Etiket listesi null veya boş döndü");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Etiket listesi alınamadı, fallback olarak ID kullanılacak");
        }
        return dict;
    }

    /// <summary>
    /// Belirli bir şirket için tüm ürün cache'lerini temizler
    /// Yeni ürün eklendiğinde veya güncellendiğinde çağrılmalı
    /// </summary>
    public void InvalidateProductCache(Guid companyId)
    {
        var keysToRemove = new List<string>();
        var prefix = $"{ProductsCacheKeyPrefix}{companyId}_";
        var singlePrefix = $"{ProductCacheKeyPrefix}{companyId}_";

        lock (_activeProductCacheKeys)
        {
            foreach (var key in _activeProductCacheKeys)
            {
                if (key.StartsWith(prefix) || key.StartsWith(singlePrefix))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _activeProductCacheKeys.Remove(key);
            }
        }

        _logger.LogInformation("Ürün cache'i temizlendi. CompanyId: {CompanyId}, Temizlenen key sayısı: {Count}", companyId, keysToRemove.Count);
    }

    /// <summary>
    /// Tüm Ticimax cache'lerini temizler (tedarikçi, etiket, ürün)
    /// </summary>
    public void InvalidateAllCache(Guid companyId, string? apiKey = null)
    {
        // Ürün cache'lerini temizle
        InvalidateProductCache(companyId);

        // Tedarikçi ve etiket cache'lerini temizle (apiKey varsa)
        if (!string.IsNullOrEmpty(apiKey))
        {
            _cache.Remove($"{TedarikciCacheKeyPrefix}{apiKey}");
            _cache.Remove($"{EtiketCacheKeyPrefix}{apiKey}");
            _logger.LogInformation("Tedarikçi ve etiket cache'i temizlendi. ApiKey: {ApiKey}", apiKey);
        }
    }

    #endregion

    /// <summary>
    /// Validates if a basket exists on Ticimax platform
    /// </summary>
    public async Task<bool> ValidateBasketExistsAsync(Guid companyId, string basketExternalId)
    {
        try
        {
            if (string.IsNullOrEmpty(basketExternalId))
                return false;

            var apiInfo = await GetTicimaxApiInfoAsync(companyId);
            if (apiInfo == null)
            {
                _logger.LogWarning("Ticimax API info not found for company {CompanyId}", companyId);
                return false;
            }

            using var client = CreateOrderServiceClient(apiInfo.Value.BaseUrl);

            // SelectSepetAsync kullanarak sepeti kontrol et
            var response = await client.SelectSepetAsync(
                apiInfo.Value.ApiKey, // UyeKodu
                0, // sepetId - 0 = tüm sepetler
                0, // uyeId - 0 = tüm üyeler
                null, // BaslangicTarihi
                null, // BitisTarihi
                1, // sayfaSayisi
                basketExternalId // guidSepetId - aradığımız sepet ID'si
            );

            // Response'da sepet bulundu mu kontrol et
            if (response?.Sepetler != null && response.Sepetler.Length > 0)
            {
                // Belirtilen GUID sepet ID'sine sahip sepet bulundu
                var foundBasket = response.Sepetler.FirstOrDefault(s => s.GuidSepetID == basketExternalId);
                return foundBasket != null;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating basket existence for basket {BasketExternalId} in company {CompanyId}", basketExternalId, companyId);
            return false;
        }
    }

    /// <summary>
    /// Creates gift voucher using global e-commerce models
    /// </summary>
    public async Task<EcommerceGiftVoucherResult> CreateGiftVoucherAsync(Guid companyId, EcommerceGiftVoucherRequest request)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return new EcommerceGiftVoucherResult
            {
                Success = false,
                Message = "Ticimax entegrasyonu bulunamadı",
                ErrorCode = "INTEGRATION_NOT_FOUND"
            };

        try
        {
            var ticimaxRequest = TicimaxMapper.MapToTicimaxGiftVoucherRequest(request);

            using var client = CreateOrderServiceClient(apiInfo.Value.BaseUrl);
            var response = await client.HediyeCekiOlusturAsync(apiInfo.Value.ApiKey, ticimaxRequest);

            return new EcommerceGiftVoucherResult
            {
                Success = !response.IsError,
                Message = response.IsError ? response.ErrorMessage : "Gift voucher created successfully",
                VoucherCode = response.HediyeCekiKodu,
                VoucherId = response.HediyeCekiId.ToString(),
                ErrorCode = response.IsError ? "TICIMAX_ERROR" : null,
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["platform"] = PlatformName,
                    ["ticimaxVoucherId"] = response.HediyeCekiId,
                    ["ticimaxVoucherCode"] = response.HediyeCekiKodu ?? string.Empty
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gift voucher in Ticimax API for company {CompanyId}", companyId);
            return new EcommerceGiftVoucherResult
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "EXCEPTION"
            };
        }
    }

    /// <summary>
    /// Tests connection using global e-commerce models
    /// </summary>
    public async Task<EcommerceConnectionResult> TestConnectionAsync(Guid companyId)
    {
        var apiInfo = await GetTicimaxApiInfoAsync(companyId);
        if (apiInfo == null)
            return new EcommerceConnectionResult
            {
                Success = false,
                Message = "Ticimax entegrasyonu bulunamadı veya yapılandırılmamış",
                PlatformName = PlatformName,
                ErrorCode = "INTEGRATION_NOT_FOUND"
            };

        try
        {
            // Basit bir webhook sorgusu ile bağlantıyı test et
            var webhooks = await GetWebhooksAsync(companyId);

            return new EcommerceConnectionResult
            {
                Success = true,
                Message = "Bağlantı başarılı",
                PlatformName = PlatformName,
                PlatformVersion = "SOAP API", // Ticimax uses SOAP
                ConnectionInfo = new Dictionary<string, object>
                {
                    ["platform"] = PlatformName,
                    ["apiType"] = "SOAP",
                    ["companyId"] = companyId,
                    ["testMethod"] = "webhook_query",
                    ["webhookCount"] = webhooks.Length
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for Ticimax API for company {CompanyId}", companyId);
            return new EcommerceConnectionResult
            {
                Success = false,
                Message = $"Bağlantı hatası: {ex.Message}",
                PlatformName = PlatformName,
                ErrorCode = "CONNECTION_FAILED"
            };
        }
    }

    /// <summary>
    /// Tests connection using provided apiUrl and apiKey (for form testing)
    /// </summary>
    public async Task<EcommerceConnectionResult> TestConnectionAsync(string apiUrl, string apiKey)
    {
        try
        {
            _logger.LogInformation("Testing Ticimax connection to {ApiUrl}", apiUrl);

            var client = CreateCustomServiceClient(apiUrl);
            var contents = await client.SelectWebhookAsync(apiKey, new SelectWebhookRequest() { ID = 0, IslemTipi = null });


            if (!contents.IsError)
            {
                return new EcommerceConnectionResult
                {
                    Success = true,
                    Message = "Ticimax API bağlantısı başarılı! API anahtarı geçerli ve servis erişilebilir.",
                    PlatformName = PlatformName,
                    PlatformVersion = "SOAP API",
                    ConnectionInfo = new Dictionary<string, object>
                    {
                        ["platform"] = PlatformName,
                        ["apiType"] = "SOAP",
                    }
                };
            }
            else
            {
                // Ticimax API hata mesajlarını kullanıcı dostu hale getir
                var errorMessage = ParseTicimaxErrorMessage(contents.ErrorMessage);

                return new EcommerceConnectionResult
                {
                    Success = false,
                    Message = errorMessage,
                    PlatformName = PlatformName,
                    ErrorCode = "API_ERROR"
                };
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP error testing Ticimax connection to {ApiUrl}", apiUrl);
            return new EcommerceConnectionResult
            {
                Success = false,
                Message = "Site adresi geçersiz veya sunucuya erişilemiyor. Lütfen URL'yi kontrol edin (örn: https://siteniz.ticimax.com).",
                PlatformName = PlatformName,
                ErrorCode = "CONNECTION_ERROR"
            };
        }
        catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
        {
            _logger.LogError(tcEx, "Timeout testing Ticimax connection to {ApiUrl}", apiUrl);
            return new EcommerceConnectionResult
            {
                Success = false,
                Message = "Sunucu yanıt vermiyor. Lütfen site adresini kontrol edin veya daha sonra tekrar deneyin.",
                PlatformName = PlatformName,
                ErrorCode = "TIMEOUT_ERROR"
            };
        }
        catch (System.ServiceModel.EndpointNotFoundException)
        {
            _logger.LogError("Endpoint not found for Ticimax connection to {ApiUrl}", apiUrl);
            return new EcommerceConnectionResult
            {
                Success = false,
                Message = "Site adresi bulunamadı. Lütfen doğru Ticimax site adresini girin (örn: https://siteniz.ticimax.com).",
                PlatformName = PlatformName,
                ErrorCode = "ENDPOINT_NOT_FOUND"
            };
        }
        catch (System.ServiceModel.ServerTooBusyException)
        {
            _logger.LogError("Server too busy for Ticimax connection to {ApiUrl}", apiUrl);
            return new EcommerceConnectionResult
            {
                Success = false,
                Message = "Ticimax sunucusu şu an meşgul veya erişilemiyor. Lütfen site adresini kontrol edin veya daha sonra tekrar deneyin.",
                PlatformName = PlatformName,
                ErrorCode = "SERVER_BUSY"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error testing Ticimax connection to {ApiUrl}", apiUrl);

            // Genel hata mesajlarını kullanıcı dostu hale getir
            var friendlyMessage = GetFriendlyErrorMessage(ex);

            return new EcommerceConnectionResult
            {
                Success = false,
                Message = friendlyMessage,
                PlatformName = PlatformName,
                ErrorCode = "UNEXPECTED_ERROR"
            };
        }
    }

    /// <summary>
    /// Ticimax API hata mesajlarını kullanıcı dostu hale getirir
    /// </summary>
    private string ParseTicimaxErrorMessage(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return "Bilinmeyen bir hata oluştu. Lütfen bilgilerinizi kontrol edin.";

        var lowerMessage = errorMessage.ToLowerInvariant();

        // API Key hataları
        if (lowerMessage.Contains("api key") || lowerMessage.Contains("apikey") ||
            lowerMessage.Contains("yetki") || lowerMessage.Contains("unauthorized") ||
            lowerMessage.Contains("authentication") || lowerMessage.Contains("invalid key"))
        {
            return "API anahtarı geçersiz. Lütfen Ticimax panelinizden doğru API anahtarını kopyalayın.";
        }

        // Erişim hataları
        if (lowerMessage.Contains("erişim") || lowerMessage.Contains("access denied") ||
            lowerMessage.Contains("forbidden") || lowerMessage.Contains("permission"))
        {
            return "Bu API anahtarı ile erişim izniniz yok. Lütfen Ticimax panelinizden API yetkilerini kontrol edin.";
        }

        // Bulunamadı hataları
        if (lowerMessage.Contains("not found") || lowerMessage.Contains("bulunamadı"))
        {
            return "Servis bulunamadı. Lütfen site adresini kontrol edin.";
        }

        return errorMessage;
    }

    /// <summary>
    /// Exception'ları kullanıcı dostu mesajlara çevirir
    /// </summary>
    private string GetFriendlyErrorMessage(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();

        if (message.Contains("name or service not known") || message.Contains("no such host"))
            return "Site adresi bulunamadı. Lütfen doğru adresi girin (örn: https://siteniz.ticimax.com).";

        if (message.Contains("connection refused"))
            return "Sunucuya bağlanılamadı. Site adresi yanlış olabilir veya sunucu kapalı olabilir.";

        if (message.Contains("ssl") || message.Contains("certificate"))
            return "Güvenli bağlantı kurulamadı. Site adresinin https:// ile başladığından emin olun.";

        if (message.Contains("timeout"))
            return "Bağlantı zaman aşımına uğradı. Lütfen daha sonra tekrar deneyin.";

        return "Bağlantı sırasında bir hata oluştu. Lütfen site adresi ve API anahtarını kontrol edin.";
    }

    #endregion
}
