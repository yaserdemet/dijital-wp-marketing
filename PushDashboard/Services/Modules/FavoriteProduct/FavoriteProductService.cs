using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Models;
using PushDashboard.Services.Integrations.Ticimax;
using PushDashboard.Services.Notifications;

namespace PushDashboard.Services.Modules.FavoriteProduct;

public class FavoriteProductService : IFavoriteProductService
{
    private readonly ApplicationDbContext _context;
    private readonly TicimaxService _ticimaxService;
    private readonly ILogger<FavoriteProductService> _logger;
    private readonly WhatsAppNotificationChannelService _whatsAppService;
    private readonly INotificationChannelFactory _channelFactory;

    public FavoriteProductService(
        ApplicationDbContext context,
        TicimaxService ticimaxService,
        ILogger<FavoriteProductService> logger,
        WhatsAppNotificationChannelService whatsAppService,
        INotificationChannelFactory channelFactory)
    {
        _context = context;
        _ticimaxService = ticimaxService;
        _logger = logger;
        _whatsAppService = whatsAppService;
        _channelFactory = channelFactory;
    }

    #region Module Settings

    public async Task<FavoriteProductSettingsDto> GetModuleSettingsAsync(Guid companyId)
    {
        try
        {
            var favoriteModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Favori Ürünler" && m.IsActive);

            if (favoriteModule == null)
            {
                return new FavoriteProductSettingsDto();
            }

            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, favoriteModule.Id);

            // Entegrasyon bilgisini al
            var integration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId
                    && ci.Integration.Name == "Ticimax"
                    && ci.IsActive
                    && ci.IsConfigured);

            var settings = new FavoriteProductSettingsDto
            {
                Enabled = moduleSettings != null && GetBoolSetting(moduleSettings, "enabled", true),
                EmailEnabled = moduleSettings != null && GetBoolSetting(moduleSettings, "emailEnabled", false),
                SmsEnabled = moduleSettings != null && GetBoolSetting(moduleSettings, "smsEnabled", false),
                WhatsAppEnabled = moduleSettings != null && GetBoolSetting(moduleSettings, "whatsAppEnabled", true),
                NotifyOnNewFavorite = moduleSettings != null && GetBoolSetting(moduleSettings, "notifyOnNewFavorite", true),
                NewFavoriteMessage = GetStringSetting(moduleSettings, "newFavoriteMessage", 
                    "Merhaba {CustomerName}, {ProductName} ürününü favorilerinize eklediniz! Fiyat: {ProductPrice}. Stokta sadece {StockCount} adet kaldı, acele edin! {ProductUrl}"),
                NotifyOnDiscount = moduleSettings != null && GetBoolSetting(moduleSettings, "notifyOnDiscount", true),
                DiscountMessage = GetStringSetting(moduleSettings, "discountMessage",
                    "Merhaba {CustomerName}, favorinizdeki {ProductName} ürünü indirimde! Yeni fiyat: {ProductPrice}. Hemen sipariş verin: {ProductUrl}"),
                // Template settings
                EmailTemplate = GetNullableStringSetting(moduleSettings, "emailTemplate"),
                EmailTemplateName = GetNullableStringSetting(moduleSettings, "emailTemplateName"),
                SmsTemplate = GetNullableStringSetting(moduleSettings, "smsTemplate"),
                SmsTemplateName = GetNullableStringSetting(moduleSettings, "smsTemplateName"),
                WhatsappTemplate = GetNullableStringSetting(moduleSettings, "whatsappTemplate"),
                WhatsappTemplateName = GetNullableStringSetting(moduleSettings, "whatsappTemplateName"),
                HasActiveEcommerceIntegration = integration != null,
                ActiveEcommercePlatform = integration?.Integration.Name
            };

            // Get available channels
            var activeChannels = await _channelFactory.GetActiveChannelsAsync(companyId);
            settings.AvailableChannels = activeChannels.Select(c => new ActiveChannelDto
            {
                ChannelType = c.ChannelType,
                DisplayName = GetChannelDisplayName(c.ChannelType),
                IsConfigured = true
            }).ToList();

            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite product module settings for company {CompanyId}", companyId);
            return new FavoriteProductSettingsDto();
        }
    }

    public async Task<bool> UpdateModuleSettingsAsync(Guid companyId, UpdateFavoriteProductSettingsRequest request)
    {
        try
        {
            var favoriteModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Favori Ürünler" && m.IsActive);

            if (favoriteModule == null)
            {
                _logger.LogWarning("Favori Ürünler module not found");
                return false;
            }

            var companyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.ModuleId == favoriteModule.Id);

            if (companyModule == null)
            {
                companyModule = new CompanyModule
                {
                    CompanyId = companyId,
                    ModuleId = favoriteModule.Id,
                    IsActive = request.Enabled,
                    PurchasedAt = DateTime.Now
                };
                _context.CompanyModules.Add(companyModule);
                await _context.SaveChangesAsync();
            }
            else
            {
                companyModule.IsActive = request.Enabled;
                companyModule.UpdatedAt = DateTime.Now;
            }

            // CompanyModuleSettings tablosuna kaydet
            var moduleSettings = await _context.CompanyModuleSettings
                .FirstOrDefaultAsync(cms => cms.CompanyModuleId == companyModule.Id);

            var settings = new Dictionary<string, object>
            {
                ["enabled"] = request.Enabled,
                ["emailEnabled"] = request.EmailEnabled,
                ["smsEnabled"] = request.SmsEnabled,
                ["whatsAppEnabled"] = request.WhatsAppEnabled,
                ["notifyOnNewFavorite"] = request.NotifyOnNewFavorite,
                ["newFavoriteMessage"] = request.NewFavoriteMessage,
                ["notifyOnDiscount"] = request.NotifyOnDiscount,
                ["discountMessage"] = request.DiscountMessage
            };

            // Add template settings if provided
            if (!string.IsNullOrEmpty(request.EmailTemplate))
                settings["emailTemplate"] = request.EmailTemplate;
            if (!string.IsNullOrEmpty(request.EmailTemplateName))
                settings["emailTemplateName"] = request.EmailTemplateName;
            if (!string.IsNullOrEmpty(request.SmsTemplate))
                settings["smsTemplate"] = request.SmsTemplate;
            if (!string.IsNullOrEmpty(request.SmsTemplateName))
                settings["smsTemplateName"] = request.SmsTemplateName;
            if (!string.IsNullOrEmpty(request.WhatsAppTemplate))
                settings["whatsappTemplate"] = request.WhatsAppTemplate;
            if (!string.IsNullOrEmpty(request.WhatsAppTemplateName))
                settings["whatsappTemplateName"] = request.WhatsAppTemplateName;

            if (moduleSettings == null)
            {
                moduleSettings = new CompanyModuleSettings
                {
                    CompanyModuleId = companyModule.Id,
                    SettingsJson = JsonSerializer.Serialize(settings),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.CompanyModuleSettings.Add(moduleSettings);
            }
            else
            {
                moduleSettings.SettingsJson = JsonSerializer.Serialize(settings);
                moduleSettings.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating favorite product module settings for company {CompanyId}", companyId);
            return false;
        }
    }

    private async Task<Dictionary<string, object>?> GetModuleSettingsByIdAsync(Guid companyId, int moduleId)
    {
        var companyModule = await _context.CompanyModules
            .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.ModuleId == moduleId);

        if (companyModule == null)
            return null;

        var moduleSettings = await _context.CompanyModuleSettings
            .FirstOrDefaultAsync(cms => cms.CompanyModuleId == companyModule.Id);

        if (moduleSettings == null || string.IsNullOrEmpty(moduleSettings.SettingsJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(moduleSettings.SettingsJson);
        }
        catch
        {
            return null;
        }
    }

    private bool GetBoolSetting(Dictionary<string, object>? settings, string key, bool defaultValue)
    {
        if (settings == null || !settings.TryGetValue(key, out var value))
            return defaultValue;

        if (value is bool boolValue)
            return boolValue;
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.True) return true;
            if (element.ValueKind == JsonValueKind.False) return false;
        }
        return defaultValue;
    }

    private string GetStringSetting(Dictionary<string, object>? settings, string key, string defaultValue)
    {
        if (settings == null || !settings.TryGetValue(key, out var value))
            return defaultValue;

        if (value is string strValue)
            return strValue;
        if (value is JsonElement element && element.ValueKind == JsonValueKind.String)
            return element.GetString() ?? defaultValue;
        return defaultValue;
    }

    private string? GetNullableStringSetting(Dictionary<string, object>? settings, string key)
    {
        if (settings == null || !settings.TryGetValue(key, out var value))
            return null;

        if (value is string strValue)
            return strValue;
        if (value is JsonElement element && element.ValueKind == JsonValueKind.String)
            return element.GetString();
        return null;
    }

    private static string GetChannelDisplayName(string channelType)
    {
        return channelType.ToLower() switch
        {
            "email" => "Email",
            "sms" => "SMS",
            "whatsapp" => "WhatsApp",
            _ => channelType
        };
    }

    #endregion

    public async Task<FavoriteProductSyncResult> SyncFavoriteProductsAsync(Guid companyId, string userId, bool sendNewFavoriteNotifications = false)
    {
        var syncLog = new FavoriteProductSyncLog
        {
            CompanyId = companyId,
            StartedAt = DateTime.Now
        };
        
        // Yeni eklenen favorileri takip etmek için
        var newFavoritesList = new List<Models.FavoriteProduct>();

        try
        {
            // Ticimax entegrasyonu kontrol et
            var integration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId
                    && ci.Integration.Name == "Ticimax"
                    && ci.IsActive
                    && ci.IsConfigured);

            if (integration == null)
            {
                syncLog.IsSuccess = false;
                syncLog.ErrorMessage = "Ticimax entegrasyonu bulunamadı.";
                syncLog.CompletedAt = DateTime.Now;
                _context.FavoriteProductSyncLogs.Add(syncLog);
                await _context.SaveChangesAsync();

                return new FavoriteProductSyncResult
                {
                    Success = false,
                    Message = "Ticimax entegrasyonu bulunamadı."
                };
            }

            // API bilgilerini al
            string apiUrl = "";
            string apiKey = "";

            if (!string.IsNullOrEmpty(integration.SettingsJson))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(integration.SettingsJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("apiUrl", out var urlProp))
                    apiUrl = urlProp.GetString() ?? "";
                if (root.TryGetProperty("apiKey", out var keyProp))
                    apiKey = keyProp.GetString() ?? "";
            }

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
            {
                syncLog.IsSuccess = false;
                syncLog.ErrorMessage = "Ticimax API bilgileri eksik.";
                syncLog.CompletedAt = DateTime.Now;
                _context.FavoriteProductSyncLogs.Add(syncLog);
                await _context.SaveChangesAsync();

                return new FavoriteProductSyncResult
                {
                    Success = false,
                    Message = "Ticimax API bilgileri eksik."
                };
            }

            // API'den favori ürünleri çek
            var client = _ticimaxService.CreateCustomServiceClient(apiUrl);
            var allFavorites = new List<CustomServis.WebFavoriUrunler>();
            int pageNumber = 1;
            const int pageSize = 500;

            while (true)
            {
                var request = new CustomServis.GetFavoriUrunlerRequest
                {
                    SayfaNo = pageNumber,
                    KayitSayisi = pageSize,
                    UyeID = 0
                };

                var response = await client.GetFavoriUrunlerAsync(apiKey, request);

                if (response.IsError)
                {
                    syncLog.IsSuccess = false;
                    syncLog.ErrorMessage = response.ErrorMessage ?? "API hatası";
                    syncLog.CompletedAt = DateTime.Now;
                    _context.FavoriteProductSyncLogs.Add(syncLog);
                    await _context.SaveChangesAsync();

                    return new FavoriteProductSyncResult
                    {
                        Success = false,
                        Message = response.ErrorMessage ?? "Favori ürünler alınamadı."
                    };
                }

                if (response.Urunler == null || response.Urunler.Length == 0)
                    break;

                allFavorites.AddRange(response.Urunler);

                if (response.Urunler.Length < pageSize)
                    break;

                pageNumber++;
            }

            syncLog.TotalFetched = allFavorites.Count;

            // Mevcut favorileri al
            var existingFavorites = await _context.FavoriteProducts
                .Where(f => f.CompanyId == companyId && f.IsActive)
                .ToDictionaryAsync(f => f.ExternalFavoriteId, f => f);

            // Müşteri bilgilerini al
            var memberIds = allFavorites.Select(f => f.UyeID).Distinct().ToList();
            var customers = await _context.Customers
                .Where(c => c.CompanyId == companyId && memberIds.Contains(c.ExternalId))
                .ToDictionaryAsync(c => c.ExternalId, c => c);

            var apiFavoriteIds = allFavorites.Select(f => f.FavoriUrunID).ToHashSet();
            int newCount = 0;
            int updateCount = 0;
            int deleteCount = 0;

            // Yeni ve güncellenen kayıtları işle
            foreach (var apiFav in allFavorites)
            {
                if (existingFavorites.TryGetValue(apiFav.FavoriUrunID, out var existing))
                {
                    // Güncelle
                    existing.ProductName = apiFav.UrunAdi ?? "";
                    existing.StockCode = apiFav.StokKodu;
                    existing.Price = (decimal)apiFav.UrunFiyati;
                    existing.PriceWithVat = (decimal)apiFav.UrunFiyatiKdv;
                    existing.Currency = apiFav.ParaBirimi ?? "TRY";
                    existing.StockQuantity = (int)apiFav.ToplamStokAdedi;
                    existing.ImageUrl = apiFav.ResimUrl;
                    existing.ProductUrl = apiFav.UrunUrl;
                    existing.VariantCount = apiFav.VaryasyonSayisi;
                    existing.UpdatedAt = DateTime.Now;
                    existing.LastSyncAt = DateTime.Now;
                    existing.IsActive = true;

                    // Müşteri eşleştir
                    if (customers.TryGetValue(apiFav.UyeID, out var customer))
                    {
                        existing.CustomerId = customer.Id;
                    }

                    updateCount++;
                }
                else
                {
                    // Yeni ekle
                    var newFavorite = new Models.FavoriteProduct
                    {
                        CompanyId = companyId,
                        ExternalFavoriteId = apiFav.FavoriUrunID,
                        ExternalProductId = apiFav.UrunKartiID,
                        ExternalMemberId = apiFav.UyeID,
                        ProductName = apiFav.UrunAdi ?? "",
                        StockCode = apiFav.StokKodu,
                        Price = (decimal)apiFav.UrunFiyati,
                        PriceWithVat = (decimal)apiFav.UrunFiyatiKdv,
                        Currency = apiFav.ParaBirimi ?? "TRY",
                        StockQuantity = (int)apiFav.ToplamStokAdedi,
                        ImageUrl = apiFav.ResimUrl,
                        ProductUrl = apiFav.UrunUrl,
                        VariantCount = apiFav.VaryasyonSayisi,
                        AddedToFavoritesAt = apiFav.EklemeTarihi,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        LastSyncAt = DateTime.Now,
                        IsActive = true
                    };

                    // Müşteri eşleştir
                    if (customers.TryGetValue(apiFav.UyeID, out var customer))
                    {
                        newFavorite.CustomerId = customer.Id;
                    }

                    _context.FavoriteProducts.Add(newFavorite);
                    newFavoritesList.Add(newFavorite); // Yeni favorileri takip et
                    newCount++;
                }
            }

            // Silinen (API'de olmayan) kayıtları pasife al
            foreach (var existing in existingFavorites.Values)
            {
                if (!apiFavoriteIds.Contains(existing.ExternalFavoriteId))
                {
                    existing.IsActive = false;
                    existing.UpdatedAt = DateTime.Now;
                    deleteCount++;
                }
            }

            // Sync log'u güncelle
            syncLog.NewRecords = newCount;
            syncLog.UpdatedRecords = updateCount;
            syncLog.DeletedRecords = deleteCount;
            syncLog.IsSuccess = true;
            syncLog.CompletedAt = DateTime.Now;

            _context.FavoriteProductSyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Favorite products synced for company {CompanyId}: {Total} fetched, {New} new, {Updated} updated, {Deleted} deleted",
                companyId, allFavorites.Count, newCount, updateCount, deleteCount);

            // Yeni favoriler için otomatik bildirim gönder (eğer aktifse)
            int notificationsSent = 0;
            if (sendNewFavoriteNotifications && newFavoritesList.Count > 0)
            {
                var notificationResult = await SendNewFavoriteNotificationsAsync(companyId, newFavoritesList, userId);
                notificationsSent = notificationResult.NotificationsSent;
                _logger.LogInformation(
                    "Sent {NotificationCount} new favorite notifications for company {CompanyId}",
                    notificationsSent, companyId);
            }

            var message = $"Senkronizasyon tamamlandı. {newCount} yeni, {updateCount} güncellenen, {deleteCount} silinen kayıt.";
            if (notificationsSent > 0)
            {
                message += $" {notificationsSent} bildirim gönderildi.";
            }

            return new FavoriteProductSyncResult
            {
                Success = true,
                Message = message,
                TotalFetched = allFavorites.Count,
                NewRecords = newCount,
                UpdatedRecords = updateCount,
                DeletedRecords = deleteCount,
                SyncedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing favorite products for company {CompanyId}", companyId);

            syncLog.IsSuccess = false;
            syncLog.ErrorMessage = ex.Message;
            syncLog.CompletedAt = DateTime.Now;
            _context.FavoriteProductSyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();

            return new FavoriteProductSyncResult
            {
                Success = false,
                Message = "Senkronizasyon sırasında hata oluştu: " + ex.Message
            };
        }
    }

    public async Task<FavoriteProductListResult> GetFavoriteProductsAsync(
        Guid companyId,
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.FavoriteProducts
                .Where(f => f.CompanyId == companyId && f.IsActive);

            // Tarih filtresi
            if (startDate.HasValue)
                query = query.Where(f => f.AddedToFavoritesAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.AddedToFavoritesAt <= endDate.Value);

            // Arama filtresi
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(f =>
                    f.ProductName.Contains(searchTerm) ||
                    (f.StockCode != null && f.StockCode.Contains(searchTerm)));
            }

            // Grupla
            var grouped = await query
                .GroupBy(f => f.ExternalProductId)
                .Select(g => new FavoriteProductGroupDto
                {
                    ProductId = g.Key,
                    ProductName = g.First().ProductName,
                    StockCode = g.First().StockCode,
                    StockQuantity = g.First().StockQuantity,
                    Price = g.First().Price,
                    PriceWithVat = g.First().PriceWithVat,
                    Currency = g.First().Currency,
                    ImageUrl = g.First().ImageUrl,
                    ProductUrl = g.First().ProductUrl,
                    VariantCount = g.First().VariantCount,
                    FavoriteCount = g.Count(),
                    FirstAddedAt = g.Min(f => f.AddedToFavoritesAt),
                    LastAddedAt = g.Max(f => f.AddedToFavoritesAt)
                })
                .OrderByDescending(g => g.FavoriteCount)
                .ToListAsync();

            var totalCount = grouped.Count;
            var totalFavorites = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedProducts = grouped
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new FavoriteProductListResult
            {
                Success = true,
                Products = pagedProducts,
                TotalCount = totalCount,
                TotalFavoriteCount = totalFavorites,
                TotalPages = totalPages,
                CurrentPage = page
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite products for company {CompanyId}", companyId);
            return new FavoriteProductListResult
            {
                Success = false,
                Message = "Favori ürünler alınırken hata oluştu."
            };
        }
    }

    public async Task<List<FavoriteProductMemberDto>> GetProductFavoriteMembersAsync(
        Guid companyId,
        int productId,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var favorites = await _context.FavoriteProducts
                .Include(f => f.Customer)
                .Where(f => f.CompanyId == companyId &&
                           f.ExternalProductId == productId &&
                           f.IsActive)
                .OrderByDescending(f => f.AddedToFavoritesAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return favorites.Select(f => new FavoriteProductMemberDto
            {
                MemberId = f.ExternalMemberId,
                MemberName = f.Customer?.FullName ?? $"Üye #{f.ExternalMemberId}",
                Email = f.Customer?.Email,
                Phone = f.Customer?.Phone ?? f.Customer?.MobilePhone,
                AddedAt = f.AddedToFavoritesAt,
                NotificationSent = f.NotificationSent,
                NotificationSentAt = f.NotificationSentAt,
                CustomerPageUrl = f.CustomerId.HasValue ? $"/Customer?search={f.ExternalMemberId}" : null
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product favorite members for company {CompanyId}", companyId);
            return new List<FavoriteProductMemberDto>();
        }
    }

    public async Task<FavoriteProductStatsResult> GetStatsAsync(Guid companyId)
    {
        try
        {
            var lastSyncInfo = await GetLastSyncAsync(companyId);
            
            // Önce veritabanından kontrol et
            var hasLocalData = await _context.FavoriteProducts
                .AnyAsync(f => f.CompanyId == companyId && f.IsActive);

            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var thisWeekStart = now.AddDays(-7);
            var todayStart = now.Date;
            
            // Veritabanından zamana dayalı istatistikleri al
            var query = _context.FavoriteProducts
                .Where(f => f.CompanyId == companyId && f.IsActive);

            var favoritesThisMonth = await query.CountAsync(f => f.AddedToFavoritesAt >= thisMonthStart);
            var favoritesThisWeek = await query.CountAsync(f => f.AddedToFavoritesAt >= thisWeekStart);
            var favoritesToday = await query.CountAsync(f => f.AddedToFavoritesAt >= todayStart);
            

            var stats = new FavoriteProductStatsResult
            {
                HasData = hasLocalData,
                TotalFavoriteProducts = await query.Select(f => f.ExternalProductId).Distinct().CountAsync(),
                TotalFavoriteCount =  await query.CountAsync(),
                TotalUniqueMembers = await query.Select(f => f.ExternalMemberId).Distinct().CountAsync(),
                FavoritesThisMonth = favoritesThisMonth,
                FavoritesThisWeek = favoritesThisWeek,
                FavoritesToday = favoritesToday,
                LastSyncAt = lastSyncInfo?.CompletedAt
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite product stats for company {CompanyId}", companyId);
            return new FavoriteProductStatsResult { HasData = false };
        }
    }

    public async Task<FavoriteProductSyncLog?> GetLastSyncAsync(Guid companyId)
    {
        return await _context.FavoriteProductSyncLogs
            .Where(s => s.CompanyId == companyId && s.IsSuccess)
            .OrderByDescending(s => s.CompletedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<FavoriteProductNotificationResult> SendNotificationAsync(
        Guid companyId,
        int productId,
        string notificationType,
        string message,
        string userId)
    {
        try
        {
            // Favori modülünün ID'sini al
            var favoriteModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Favori Ürünler");
            int? moduleId = favoriteModule?.Id;

            // Ürün bilgisini al
            var productInfo = await _context.FavoriteProducts
                .Where(f => f.CompanyId == companyId && f.ExternalProductId == productId && f.IsActive)
                .Select(f => new { f.ProductName, f.ProductUrl, f.PriceWithVat, f.Currency })
                .FirstOrDefaultAsync();

            if (productInfo == null)
            {
                return new FavoriteProductNotificationResult
                {
                    Success = false,
                    Message = "Ürün bulunamadı."
                };
            }

            // Bildirim gönderilmemiş favorileri al
            var favorites = await _context.FavoriteProducts
                .Include(f => f.Customer)
                .Where(f => f.CompanyId == companyId &&
                           f.ExternalProductId == productId &&
                           f.IsActive &&
                           !f.NotificationSent &&
                           f.Customer != null)
                .ToListAsync();

            if (!favorites.Any())
            {
                return new FavoriteProductNotificationResult
                {
                    Success = true,
                    Message = "Bildirim gönderilecek müşteri bulunamadı.",
                    NotificationsSent = 0
                };
            }

            int successCount = 0;
            int failCount = 0;
            decimal totalCost = 0;

            // Modül ayarlarını al (template bilgileri için)
            var moduleSettings = moduleId.HasValue ? await GetModuleSettingsByIdAsync(companyId, moduleId.Value) : null;

            // Kanal aktifliklerini al
            bool emailEnabled = GetBoolSetting(moduleSettings, "emailEnabled", false);
            bool smsEnabled = GetBoolSetting(moduleSettings, "smsEnabled", false);
            bool whatsAppEnabled = GetBoolSetting(moduleSettings, "whatsAppEnabled", true);

            // Template isimlerini al
            string? emailTemplate = GetNullableStringSetting(moduleSettings, "emailTemplate");
            string? smsTemplate = GetNullableStringSetting(moduleSettings, "smsTemplate");
            string? whatsAppTemplate = GetNullableStringSetting(moduleSettings, "whatsappTemplate");

            // Aktif kanalları bir kere çek
            var activeChannels = await _channelFactory.GetActiveChannelsAsync(companyId);

            // Kanal -> template eşleştirmesi
            var channelTemplates = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["email"] = emailEnabled ? emailTemplate : null,
                ["sms"] = smsEnabled ? smsTemplate : null,
                ["whatsapp"] = whatsAppEnabled ? whatsAppTemplate : null
            };

            foreach (var fav in favorites)
            {
                if (fav.Customer == null) continue;

                bool sent = false;
                
                // Template değişkenleri hazırla
                var variables = new Dictionary<string, string>
                {
                    ["customer_name"] = fav.Customer.FullName ?? fav.Customer.FirstName ?? "Değerli Müşterimiz",
                    ["customer_first_name"] = fav.Customer.FirstName ?? "",
                    ["customer_last_name"] = fav.Customer.LastName ?? "",
                    ["customer_email"] = fav.Customer.Email ?? "",
                    ["customer_phone"] = fav.Customer.Phone ?? fav.Customer.MobilePhone ?? "",
                    ["current_date"] = DateTime.Now.ToString("dd.MM.yyyy"),
                    ["current_time"] = DateTime.Now.ToString("HH:mm"),
                    ["product_name"] = productInfo.ProductName ?? "",
                    ["product_price"] = $"{productInfo.PriceWithVat:N2} {productInfo.Currency}",
                    ["product_url"] = productInfo.ProductUrl ?? "",
                };

                // Aktif kanallar üzerinde dön
                foreach (var channel in activeChannels)
                {
                    // Kanal için template var mı kontrol et
                    if (!channelTemplates.TryGetValue(channel.ChannelType, out var template) || string.IsNullOrEmpty(template))
                        continue;

                    // Kanal tipine göre ek kontroller
                    if (channel.ChannelType.Equals("email", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(fav.Customer.Email))
                        continue;
                    
                    if (channel.ChannelType.Equals("sms", StringComparison.OrdinalIgnoreCase))
                    {
                        var phoneNumber = fav.Customer.Phone ?? fav.Customer.MobilePhone;
                        if (string.IsNullOrEmpty(phoneNumber))
                            continue;
                    }

                    try
                    {
                        var channelSent = await channel.SendNotificationWithCostTrackingAsync(
                            companyId,
                            fav.Customer,
                            template,
                            variables,
                            userId,
                            moduleId,
                            $"fav_{fav.Id}_{channel.ChannelType.ToLower()}");
                        
                        if (channelSent) sent = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "{ChannelType} notification failed for customer {CustomerId}", 
                            channel.ChannelType, fav.Customer.Id);
                    }
                }

                if (sent)
                {
                    successCount++;
                    fav.NotificationSent = true;
                    fav.NotificationSentAt = DateTime.Now;
                }
                else
                {
                    failCount++;
                }
            }

            await _context.SaveChangesAsync();

            var resultMessage = successCount > 0
                ? $"{successCount} müşteriye bildirim gönderildi."
                : "Bildirim gönderilemedi.";

            if (failCount > 0)
            {
                resultMessage += $" ({failCount} başarısız)";
            }

            _logger.LogInformation(
                "Favorite product notification sent for product {ProductId}: {Success} success, {Fail} fail",
                productId, successCount, failCount);

            return new FavoriteProductNotificationResult
            {
                Success = successCount > 0,
                Message = resultMessage,
                NotificationsSent = successCount,
                TotalCost = totalCost
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for product {ProductId}", productId);
            return new FavoriteProductNotificationResult
            {
                Success = false,
                Message = "Bildirim gönderilirken hata oluştu: " + ex.Message
            };
        }
    }

    /// <summary>
    /// Yeni eklenen favoriler için otomatik bildirim gönderir
    /// </summary>
    public async Task<FavoriteProductNotificationResult> SendNewFavoriteNotificationsAsync(
        Guid companyId,
        List<Models.FavoriteProduct> newFavorites,
        string userId)
    {
        try
        {
            if (newFavorites == null || newFavorites.Count == 0)
            {
                return new FavoriteProductNotificationResult
                {
                    Success = true,
                    Message = "Gönderilecek yeni favori yok.",
                    NotificationsSent = 0
                };
            }

            // Modül ayarlarını al
            var settings = await GetModuleSettingsAsync(companyId);
            
            // Yeni favori bildirimi kapalıysa çık
            if (!settings.NotifyOnNewFavorite || !settings.WhatsAppEnabled)
            {
                return new FavoriteProductNotificationResult
                {
                    Success = true,
                    Message = "Yeni favori bildirimi ayarlardan kapalı.",
                    NotificationsSent = 0
                };
            }

            // Favori modülünün ID'sini al
            var favoriteModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Favori Ürünler");
            int? moduleId = favoriteModule?.Id;

            // WhatsApp konfigürasyonu kontrol et
            var whatsAppConfigured = await _whatsAppService.IsConfiguredAsync(companyId);
            if (!whatsAppConfigured)
            {
                _logger.LogWarning("WhatsApp not configured for company {CompanyId}, skipping new favorite notifications", companyId);
                return new FavoriteProductNotificationResult
                {
                    Success = false,
                    Message = "WhatsApp konfigürasyonu bulunamadı.",
                    NotificationsSent = 0
                };
            }

            int successCount = 0;
            int failCount = 0;
            decimal totalCost = 0;

            // Mesaj template'ini ayarlardan al
            var messageTemplate = settings.NewFavoriteMessage;

            foreach (var favorite in newFavorites)
            {
                // Sadece müşteri eşleştirilmiş favorilere bildirim gönder
                if (!favorite.CustomerId.HasValue)
                    continue;

                var customer = await _context.Customers.FindAsync(favorite.CustomerId.Value);
                if (customer == null)
                    continue;

                try
                {
                    // Mesajı oluştur (template'den)
                    var customerName = customer.FullName ?? customer.FirstName ?? "Değerli Müşterimiz";
                    var message = messageTemplate
                        .Replace("{CustomerName}", customerName)
                        .Replace("{ProductName}", favorite.ProductName)
                        .Replace("{ProductPrice}", $"{favorite.PriceWithVat:N2} {favorite.Currency}")
                        .Replace("{StockCount}", favorite.StockQuantity.ToString())
                        .Replace("{ProductUrl}", favorite.ProductUrl ?? "");

                    // Template değişkenleri hazırla (WhatsApp için)
                    var variables = new Dictionary<string, string>
                    {
                        ["customer_name"] = customerName,
                        ["product_name"] = favorite.ProductName,
                        ["product_price"] = $"{favorite.PriceWithVat:N2} {favorite.Currency}",
                        ["stock_count"] = favorite.StockQuantity.ToString(),
                        ["product_url"] = favorite.ProductUrl ?? "",
                        ["message"] = message
                    };

                    var sent = await _whatsAppService.SendNotificationWithCostTrackingAsync(
                        companyId,
                        customer,
                        "favorite_added_notification", // Yeni favori eklendi template'i
                        variables,
                        userId,
                        moduleId,
                        $"new_fav_{favorite.Id}");

                    if (sent)
                    {
                        successCount++;
                        totalCost += 1.00m;
                        favorite.NotificationSent = true;
                        favorite.NotificationSentAt = DateTime.Now;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send new favorite notification for favorite {FavoriteId}", favorite.Id);
                    failCount++;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "New favorite notifications sent for company {CompanyId}: {Success} success, {Fail} fail",
                companyId, successCount, failCount);

            return new FavoriteProductNotificationResult
            {
                Success = successCount > 0,
                Message = successCount > 0 
                    ? $"{successCount} yeni favori bildirimi gönderildi." 
                    : "Bildirim gönderilemedi.",
                NotificationsSent = successCount,
                TotalCost = totalCost
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending new favorite notifications for company {CompanyId}", companyId);
            return new FavoriteProductNotificationResult
            {
                Success = false,
                Message = "Bildirim gönderilirken hata oluştu: " + ex.Message
            };
        }
    }

    /// <summary>
    /// Webhook'tan gelen favori ekleme eventini işler
    /// </summary>
    public async Task<bool> ProcessFavoriteAddedWebhookAsync(
        Guid companyId,
        int externalProductId,
        int externalMemberId,
        string productName,
        string? productUrl)
    {
        try
        {
            _logger.LogInformation(
                "Processing FavoriteAdded webhook for company {CompanyId}, product {ProductId}, member {MemberId}",
                companyId, externalProductId, externalMemberId);

            // Müşteriyi bul
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.ExternalId == externalMemberId);

            if (customer == null)
            {
                _logger.LogWarning(
                    "Customer not found for FavoriteAdded webhook: ExternalMemberId {MemberId}, Company {CompanyId}",
                    externalMemberId, companyId);
                return false;
            }

            // Favori modülünün ID'sini al
            var favoriteModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Favori Ürünler");
            int? moduleId = favoriteModule?.Id;

            // WhatsApp konfigürasyonu kontrol et
            var whatsAppConfigured = await _whatsAppService.IsConfiguredAsync(companyId);
            if (!whatsAppConfigured)
            {
                _logger.LogWarning("WhatsApp not configured for company {CompanyId}", companyId);
                return false;
            }

            // Template değişkenleri hazırla
            var variables = new Dictionary<string, string>
            {
                ["customer_name"] = customer.FullName ?? customer.FirstName ?? "Değerli Müşterimiz",
                ["product_name"] = productName,
                ["product_url"] = productUrl ?? ""
            };

            // Bildirim gönder
            var sent = await _whatsAppService.SendNotificationWithCostTrackingAsync(
                companyId,
                customer,
                "favorite_added_notification",
                variables,
                "webhook",
                moduleId,
                $"webhook_fav_{externalProductId}_{externalMemberId}");

            if (sent)
            {
                _logger.LogInformation(
                    "FavoriteAdded notification sent successfully for company {CompanyId}, customer {CustomerId}",
                    companyId, customer.Id);
            }

            return sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error processing FavoriteAdded webhook for company {CompanyId}", 
                companyId);
            return false;
        }
    }
}

