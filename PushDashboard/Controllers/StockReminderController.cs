using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Filters;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.Services.Integrations.Ticimax;
using PushDashboard.Services.Modules.StockReminder;
using Microsoft.EntityFrameworkCore;

namespace PushDashboard.Controllers;

[Authorize]
public class StockReminderController : BaseController
{
    private readonly IStockReminderModuleService _stockReminderModuleService;
    private readonly TicimaxService _ticimaxService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<StockReminderController> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IStockReminderWidgetService _widgetService;

    public StockReminderController(
        IStockReminderModuleService stockReminderModuleService,
        TicimaxService ticimaxService,
        IActivityLogService activityLogService,
        ILogger<StockReminderController> logger,
        ApplicationDbContext context,
        IUserContextService userContextService,
        IStockReminderWidgetService widgetService) : base(userContextService, context)
    {
        _stockReminderModuleService = stockReminderModuleService;
        _ticimaxService = ticimaxService;
        _activityLogService = activityLogService;
        _logger = logger;
        _dbContext = context;
        _widgetService = widgetService;
    }

    /// <summary>
    /// Stok hatırlatma ana sayfa
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction("Index", "Home");
        }

        try
        {
            var settings = await _stockReminderModuleService.GetModuleSettingsAsync(companyId.Value);
            var stats = await _stockReminderModuleService.GetModuleStatsAsync(companyId.Value);
            string? siteUrl = null;
            var integration = await _dbContext.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId.Value
                    && ci.Integration.Name == "Ticimax"
                    && ci.IsActive
                    && ci.IsConfigured);

            if (integration != null && !string.IsNullOrEmpty(integration.SettingsJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(integration.SettingsJson);
                    if (doc.RootElement.TryGetProperty("apiUrl", out var apiUrlProp))
                    {
                        siteUrl = apiUrlProp.GetString();
                    }
                }
                catch { }
            }
            
            var viewModel = new StockReminderIndexViewModel
            {
                Settings = settings,
                Stats = stats,
                SiteUrl = siteUrl
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stock reminder page for company {CompanyId}", companyId);
            TempData["ErrorMessage"] = "Sayfa yüklenirken bir hata oluştu.";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Stok alarm ürünlerini getirir (Ticimax API)
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetStokAlarmUrunler(int page = 1, int pageSize = 20, int? minStock = null, string? searchTerm = null)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            // Ticimax API bilgilerini al
            var integration = await _dbContext.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId 
                    && ci.Integration.Name == "Ticimax" 
                    && ci.IsActive 
                    && ci.IsConfigured);

            if (integration == null)
            {
                return Json(new { success = false, message = "Ticimax entegrasyonu bulunamadı veya yapılandırılmamış." });
            }

            // Settings'i güvenli şekilde parse et
            string apiUrl = "";
            string apiKey = "";
            
            if (!string.IsNullOrEmpty(integration.SettingsJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(integration.SettingsJson);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("apiUrl", out var urlProp))
                        apiUrl = urlProp.GetString() ?? "";
                    
                    if (root.TryGetProperty("apiKey", out var keyProp))
                        apiKey = keyProp.GetString() ?? "";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse SettingsJson for company {CompanyId}", companyId);
                }
            }

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
            {
                return Json(new { success = false, message = "Ticimax API bilgileri eksik." });
            }

            // CustomServis client oluştur ve stok alarm ürünlerini çek
            var client = _ticimaxService.CreateCustomServiceClient(apiUrl);
            
            var request = new CustomServis.GetStokAlarmUrunlerRequest
            {
                UrunKartiId = 0, // Tüm ürünler
                UyeID = 0 // Tüm üyeler
            };

            var response = await client.GetStokAlarmUrunlerAsync(apiKey, request);

            if (response.IsError)
            {
                _logger.LogWarning("Ticimax GetStokAlarmUrunler error: {Error}", response.ErrorMessage);
                return Json(new { success = false, message = response.ErrorMessage ?? "Stok alarm ürünleri alınamadı." });
            }

            var urunler = response.Urunler?
                .OrderByDescending(u => u.EklemeTarihi)
                .ToList() ?? new List<CustomServis.WebStokAlarmUrunler>();

            // Filtreleme
            if (minStock.HasValue)
            {
                urunler = urunler.Where(u => u.ToplamStokAdedi >= minStock.Value).ToList();
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                urunler = urunler.Where(u => 
                    (u.UrunAdi?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.StokKodu?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            var memberIds = urunler.Select(u => u.UyeID).Distinct().ToList();
            var customers = await _dbContext.Customers
                .Where(c => c.CompanyId == companyId && memberIds.Contains(c.ExternalId))
                .ToDictionaryAsync(c => c.ExternalId, c => c);

            // Sayfalama
            var totalCount = urunler.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedUrunler = urunler
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u =>
                {
                    var customer = customers.GetValueOrDefault(u.UyeID);
                    return new StokAlarmUrunDto
                    {
                        StokAlarmUrunID = u.StokAlarmUrunID,
                        UrunKartiID = u.UrunKartiID,
                        UrunAdi = u.UrunAdi,
                        StokKodu = u.StokKodu,
                        ToplamStokAdedi = (int)u.ToplamStokAdedi,
                        UrunFiyati = u.UrunFiyati,
                        UrunFiyatiKdv = u.UrunFiyatiKdv,
                        ParaBirimi = u.ParaBirimi,
                        ResimUrl = u.ResimUrl,
                        UrunUrl = u.UrunUrl,
                        VaryasyonSayisi = u.VaryasyonSayisi,
                        UyeID = u.UyeID,
                        EklemeTarihi = u.EklemeTarihi,
                        CustomerName = customer?.FullName ?? $"Üye #{u.UyeID}",
                        CustomerEmail = customer?.Email
                    };
                })
                .ToList();

            return Json(new
            {
                success = true,
                data = pagedUrunler,
                totalCount,
                totalPages,
                currentPage = page,
                hasMore = page < totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock alarm products for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Stok alarm ürünleri alınırken hata oluştu." });
        }
    }

    /// <summary>
    /// Veritabanından kayıtlı stok alarm ürünlerini ürün bazında gruplayarak getirir (StockReminderWidgetSubmissions tablosundan)
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetStockReminderItems(int page = 1, int pageSize = 20, int? minStock = null, string? searchTerm = null, bool? notified = null, bool? inStock = null)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var settings = await _stockReminderModuleService.GetModuleSettingsAsync(companyId);
            int threshold = settings?.StockThreshold ?? 1;

            var query = _dbContext.StockReminderWidgetSubmissions
                .Where(s => s.CompanyId == companyId && s.ProductId != null)
                .AsQueryable();

            // Filtreler
            if (inStock.HasValue)
            {
                if (inStock.Value)
                    query = query.Where(s => s.TotalStock >= threshold);
                else
                    query = query.Where(s => s.TotalStock < threshold);
            }
            
            if (minStock.HasValue)
            {
                // minStock filtresi varsa inStock'u eziyor olabilir ya da mantıken beraber çalışması gerekir
                query = query.Where(s => s.TotalStock >= minStock.Value);
            }

            if (notified.HasValue)
            {
                query = query.Where(s => s.IsNotified == notified.Value);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s =>
                    (s.ProductName != null && s.ProductName.Contains(searchTerm)) ||
                    (s.StockCode != null && s.StockCode.Contains(searchTerm)) ||
                    (s.FirstName != null && s.FirstName.Contains(searchTerm)) ||
                    (s.LastName != null && s.LastName.Contains(searchTerm)) ||
                    (s.Phone != null && s.Phone.Contains(searchTerm)));
            }

            // Veritabanında ürün bazında grupla (hafif sorgu — sadece aggregate)
            var groupedQuery = query
                .GroupBy(s => s.ProductId!)
                .Select(g => new
                {
                    UrunKartiID = g.Key,
                    UrunAdi = g.Max(s => s.ProductName) ?? "",
                    StokKodu = g.Max(s => s.StockCode),
                    ToplamStokAdedi = g.Max(s => s.TotalStock),
                    UrunFiyati = g.Max(s => s.Price),
                    UrunFiyatiKdv = g.Max(s => s.PriceWithTax),
                    ParaBirimi = g.Max(s => s.Currency),
                    ResimUrl = g.Max(s => s.ImageUrl),
                    UrunUrl = g.Max(s => s.ProductUrl),
                    AboneSayisi = g.Count(),
                    BildirimGonderilen = g.Count(x => x.IsNotified),
                    BildirimBekleyen = g.Count(x => !x.IsNotified),
                    LastSyncedAt = g.Max(s => s.LastSyncedAt),
                    IlkKayitTarihi = g.Min(s => s.CreatedAt)
                });

            var totalCount = await groupedQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Filtrelenmiş tüm ürünlerin toplam abone sayılarını hesapla (sayfalama öncesi)
            var subscriberTotals = await groupedQuery
                .GroupBy(x => 1)
                .Select(g => new
                {
                    TotalSubscribers = g.Sum(x => x.AboneSayisi),
                    TotalPending = g.Sum(x => x.BildirimBekleyen)
                })
                .FirstOrDefaultAsync();

            var totalSubscribers = subscriberTotals?.TotalSubscribers ?? 0;
            var totalPendingSubscribers = subscriberTotals?.TotalPending ?? 0;

            // Sayfalanmış ürün gruplarını çek
            var pagedProducts = await groupedQuery
                .OrderByDescending(p => p.LastSyncedAt)
                .ThenByDescending(p => p.IlkKayitTarihi)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Sadece bu sayfadaki ürünlerin abonelerini çek
            var pagedProductIds = pagedProducts.Select(p => p.UrunKartiID).ToList();
            var subscribers = await _dbContext.StockReminderWidgetSubmissions
                .Where(s => s.CompanyId == companyId && s.ProductId != null && pagedProductIds.Contains(s.ProductId))
                .Select(s => new
                {
                    s.ProductId,
                    s.Id,
                    Ad = (s.FirstName ?? "") + " " + (s.LastName ?? ""),
                    s.Email,
                    Telefon = s.Phone,
                    Kaynak = s.Source,
                    KayitTarihi = s.CreatedAt,
                    BildirimGonderildi = s.IsNotified,
                    BildirimTarihi = s.NotifiedAt
                })
                .OrderByDescending(s => s.KayitTarihi)
                .ToListAsync();

            var subscribersByProduct = subscribers
                .GroupBy(s => s.ProductId!)
                .ToDictionary(g => g.Key, g => g.Select(s => new
                {
                    s.Id,
                    ad = s.Ad.Trim(),
                    s.Email,
                    telefon = s.Telefon,
                    kaynak = s.Kaynak,
                    kayitTarihi = s.KayitTarihi,
                    bildirimGonderildi = s.BildirimGonderildi,
                    bildirimTarihi = s.BildirimTarihi
                }).ToList());

            // Sonuçları birleştir
            var result = pagedProducts.Select(p => new
            {
                urunKartiID = p.UrunKartiID,
                urunAdi = p.UrunAdi,
                stokKodu = p.StokKodu,
                toplamStokAdedi = p.ToplamStokAdedi,
                urunFiyati = p.UrunFiyati,
                urunFiyatiKdv = p.UrunFiyatiKdv,
                paraBirimi = p.ParaBirimi,
                resimUrl = p.ResimUrl,
                urunUrl = p.UrunUrl,
                aboneSayisi = p.AboneSayisi,
                bildirimGonderilen = p.BildirimGonderilen,
                bildirimBekleyen = p.BildirimBekleyen,
                lastSyncedAt = p.LastSyncedAt,
                ilkKayitTarihi = p.IlkKayitTarihi,
                aboneler = subscribersByProduct.GetValueOrDefault(p.UrunKartiID, new())
            }).ToList();

            // Son senkronizasyon zamanını bul
            var lastSyncTime = await _dbContext.StockReminderWidgetSubmissions
                .Where(s => s.CompanyId == companyId && s.LastSyncedAt != null)
                .MaxAsync(s => (DateTime?)s.LastSyncedAt);

            return Json(new
            {
                success = true,
                data = result,
                totalCount,
                totalPages,
                currentPage = page,
                hasMore = page < totalPages,
                lastSyncedAt = lastSyncTime,
                totalSubscribers,
                totalPendingSubscribers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock reminder items from DB for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Stok alarm verileri alınırken hata oluştu." });
        }
    }

    /// <summary>
    /// Stok alarm ayarlarını günceller
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    [RequireUser]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateStockReminderSettingsRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId()!;

        try
        {
            var (success, message) = await _stockReminderModuleService.UpdateModuleSettingsAsync(companyId, request, userId);

            if (success)
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId,
                    ActivityActionTypes.Update,
                    ActivityModules.StockReminder,
                    "Stok hatırlatma ayarları güncellendi",
                    entityType: "StockReminderSettings",
                    details: new { request.Enabled, request.StockThreshold },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());

                return Json(new { success = true, message });
            }

            return Json(new { success = false, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock reminder settings for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Ayarlar güncellenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Seçilen ürünler için stok bildirimi gönderir
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    [RequireUser]
    public async Task<IActionResult> SendStockNotification([FromBody] SendStockNotificationRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId()!;

        try
        {
            if (request.ProductIds == null || !request.ProductIds.Any())
            {
                return Json(new { success = false, message = "Lütfen en az bir ürün seçin." });
            }

            var results = new List<object>();
            var totalNotificationsSent = 0;
            var totalCost = 0m;

            foreach (var productId in request.ProductIds)
            {
                var (success, message, notificationsSent, cost) = await _stockReminderModuleService
                    .SendStockReminderNotificationAsync(companyId, productId, request.CurrentStock ?? 0, userId);

                results.Add(new { productId, success, message, notificationsSent, cost });
                
                if (success)
                {
                    totalNotificationsSent += notificationsSent;
                    totalCost += cost;
                }
            }

            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Send,
                ActivityModules.StockReminder,
                $"Stok bildirimi gönderildi ({request.ProductIds.Count} ürün)",
                entityType: "StockNotification",
                details: new { productCount = request.ProductIds.Count, totalNotificationsSent, totalCost },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new
            {
                success = true,
                message = $"{totalNotificationsSent} bildirim gönderildi.",
                results,
                totalNotificationsSent,
                totalCost
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending stock notification for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Bildirim gönderilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Aktif iletişim kanallarını getirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetActiveChannels()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var channels = await _stockReminderModuleService.GetActiveChannelsAsync(companyId);
            return Json(new { success = true, channels });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active channels for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Kanallar yüklenirken hata oluştu.", channels = new List<ActiveChannelDto>() });
        }
    }

    /// <summary>
    /// Stok alarm senkronizasyonunu aktivite loguna kaydeder (Senkronize Et butonu)
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    [RequireUser]
    public async Task<IActionResult> LogStockAlarmSync()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId()!;

        try
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Sync,
                ActivityModules.StockReminder,
                "Stok alarm senkronizasyonu yapıldı",
                entityType: "StockAlarmSync",
                details: new { Source = "Senkronize Et" },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging stock alarm sync for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Log kaydı alınamadı." });
        }
    }

    /// <summary>
    /// Manuel olarak stok geldi bildirimi gönderir (seçili ürünler için)
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    [RequireUser]
    public async Task<IActionResult> SendStockArrivedNotification([FromBody] SendStockArrivedNotificationRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId()!;

        try
        {
            if (!request.Products.Any())
            {
                return Json(new { success = false, message = "Lütfen en az bir ürün seçin." });
            }

            var results = new List<object>();
            var totalNotificationsSent = 0;
            var totalCost = 0m;

            foreach (var product in request.Products)
            {
                var (success, message, notificationsSent, cost) = await _stockReminderModuleService
                    .SendStockArrivedNotificationsAsync(
                        companyId, 
                        product.ProductId, 
                        product.ProductName ?? "Ürün",
                        product.ProductUrl,
                        product.CurrentStock,
                        userId);

                results.Add(new { productId = product.ProductId, productName = product.ProductName, success, message, notificationsSent, cost });
                
                if (success)
                {
                    totalNotificationsSent += notificationsSent;
                    totalCost += cost;
                }
            }

            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Send,
                ActivityModules.StockReminder,
                $"Stok geldi bildirimi gönderildi ({request.Products.Count} ürün)",
                entityType: "StockArrivedNotification",
                details: new { productCount = request.Products.Count, totalNotificationsSent, totalCost },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            if (totalNotificationsSent > 0)
            {
                return Json(new
                {
                    success = true,
                    message = $"{totalNotificationsSent} aboneye stok geldi bildirimi gönderildi.",
                    results,
                    totalNotificationsSent,
                    totalCost
                });
            }

            return Json(new
            {
                success = false,
                message = "Bildirim gönderilecek abone bulunamadı.",
                results,
                totalNotificationsSent = 0,
                totalCost = 0m
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending stock arrived notification for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Bildirim gönderilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Belirli bir ürün için stok alarm abonelerini getirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetStockAlarmSubscribers(int productId)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var subscribers = await _stockReminderModuleService.GetStockAlarmSubscribersAsync(companyId, productId);
            return Json(new { success = true, data = subscribers, count = subscribers.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock alarm subscribers for product {ProductId}", productId);
            return Json(new { success = false, message = "Aboneler yüklenirken hata oluştu.", data = new List<StockAlarmSubscriberDto>() });
        }
    }

    /* ═══════════════════════════════════════════════════════
       WIDGET AYARLARI (Müşteri ön yüzü popup form)
    ═══════════════════════════════════════════════════════ */

    /// <summary>
    /// Stok hatırlatma widget ayarları sayfası (ayrı sayfa)
    /// </summary>
    [RequireCompanyId]
    public async Task<IActionResult> WidgetSettings()
    {
        return View();
    }

    /// <summary>
    /// Stok hatırlatma widget ayarlarını getirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetWidgetSettings()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        try
        {
            var settings = await _widgetService.GetWidgetSettingsAsync(companyId);
            if (settings == null)
            {
                var userId = GetCurrentUserId()!;
                settings = await _widgetService.CreateDefaultSettingsAsync(companyId, userId);
                await _widgetService.SaveWidgetSettingsAsync(settings);
                settings = await _widgetService.GetWidgetSettingsAsync(companyId);
            }
            // Fetch legal text links from Integration Settings (same logic as GetScriptConfigAsync)
            string? kvkkLink = null;
            string? etkLink = null;
            try
            {
                var ecommerceIntegration = await _dbContext.CompanyIntegrations
                    .Include(ci => ci.Integration)
                    .Where(ci => ci.CompanyId == companyId
                                 && ci.IsActive
                                 && ci.IsConfigured
                                 && ci.Integration.Type == "ecommerce")
                    .FirstOrDefaultAsync();

                if (ecommerceIntegration != null)
                {
                    kvkkLink = ecommerceIntegration.KvkkLink;
                    etkLink = ecommerceIntegration.TicariElektronikIletiLink;

                    if (string.IsNullOrEmpty(kvkkLink) && !string.IsNullOrEmpty(ecommerceIntegration.SettingsJson))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(ecommerceIntegration.SettingsJson);
                            var root = doc.RootElement;
                            if ((root.TryGetProperty("KvkkLink", out var kProp) || root.TryGetProperty("kvkkLink", out kProp)) &&
                                kProp.ValueKind == System.Text.Json.JsonValueKind.String)
                                kvkkLink = kProp.GetString();
                        }
                        catch { }
                    }

                    if (string.IsNullOrEmpty(etkLink) && !string.IsNullOrEmpty(ecommerceIntegration.SettingsJson))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(ecommerceIntegration.SettingsJson);
                            var root = doc.RootElement;
                            if ((root.TryGetProperty("EtkLink", out var eProp) || root.TryGetProperty("ticariElektronikIletiLink", out eProp)) &&
                                eProp.ValueKind == System.Text.Json.JsonValueKind.String)
                                etkLink = eProp.GetString();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch legal links for stock reminder widget settings. CompanyId: {CompanyId}", companyId);
            }

            // Project to flat DTO to avoid EF navigation property circular reference
            var dto = new
            {
                settings!.Id,
                settings.Title,
                settings.Description,
                settings.SubmitButtonText,
                settings.PrivacyText,
                KvkkLink = kvkkLink,
                EtkLink = etkLink,
                settings.PhonePlaceholder,
                settings.EmailPlaceholder,
                settings.VariantOptions,
                settings.VariantLabel,
                settings.ShowVariantSelector,
                settings.ShowPhoneField,
                settings.ShowEmailField,
                settings.ShowNameFields,
                settings.RequirePhone,
                settings.ShowConsentCheckbox,
                settings.BackgroundColor,
                settings.TextColor,
                settings.TitleColor,
                settings.ButtonColor,
                settings.ButtonTextColor,
                settings.VariantBorderColor,
                settings.VariantSelectedColor,
                settings.BorderRadius,
                settings.IsActive,
                settings.ShowOnMobile,
                settings.ShowOnDesktop,
                settings.EnableAnimation,
                settings.ImpressionCount,
                settings.SubmitCount,
                settings.DismissCount,
                settings.UpdatedAt
            };
            return Json(new { success = true, data = dto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting widget settings for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Widget ayarları yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Stok hatırlatma widget ayarlarını kaydeder
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    [RequireUser]
    public async Task<IActionResult> SaveWidgetSettings([FromBody] StockReminderWidgetSaveModel model)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId()!;

        try
        {
            var settings = new PushDashboard.Models.StockReminderWidgetSettings
            {
                CompanyId = companyId,
                Title = model.Title,
                Description = model.Description,
                SubmitButtonText = model.SubmitButtonText,
                // PrivacyText ve PrivacyUrl entegrasyon ayarlarından yönetilmektedir
                PhonePlaceholder = model.PhonePlaceholder,
                EmailPlaceholder = model.EmailPlaceholder,
                VariantOptions = model.VariantOptions,
                VariantLabel = model.VariantLabel,
                ShowVariantSelector = model.ShowVariantSelector,
                ShowPhoneField = model.ShowPhoneField,
                ShowEmailField = model.ShowEmailField,
                ShowNameFields = model.ShowNameFields,
                RequirePhone = model.RequirePhone,
                ShowConsentCheckbox = model.ShowConsentCheckbox,
                BackgroundColor = model.BackgroundColor,
                TextColor = model.TextColor,
                TitleColor = model.TitleColor,
                ButtonColor = model.ButtonColor,
                ButtonTextColor = model.ButtonTextColor,
                VariantBorderColor = model.VariantBorderColor,
                VariantSelectedColor = model.VariantSelectedColor,
                BorderRadius = model.BorderRadius,
                IsActive = model.IsActive,
                ShowOnMobile = model.ShowOnMobile,
                ShowOnDesktop = model.ShowOnDesktop,
                EnableAnimation = model.EnableAnimation,
                UpdatedByUserId = userId
            };

            var result = await _widgetService.SaveWidgetSettingsAsync(settings);

            if (result)
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId,
                    ActivityActionTypes.Update,
                    ActivityModules.StockReminder,
                    "Stok hatırlatma widget ayarları güncellendi",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());

                return Json(new { success = true, message = "Widget ayarları başarıyla kaydedildi." });
            }

            return Json(new { success = false, message = "Ayarlar kaydedilirken bir hata oluştu." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving widget settings for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
        }
    }

    /// <summary>
    /// Widget konfigürasyonunu R2'ye export eder (siteye yayınla)
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    [RequireUser]
    public async Task<IActionResult> ExportWidgetToR2()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId()!;

        try
        {
            var url = await _widgetService.ExportWidgetToR2Async(companyId);

            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Update,
                ActivityModules.StockReminder,
                "Stok hatırlatma widget R2'ye yayınlandı",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new { success = true, message = "Widget başarıyla yayınlandı.", url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting widget to R2 for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Yayınlama sırasında bir hata oluştu: " + ex.Message });
        }
    }

    /// <summary>
    /// Widget önizleme verisi (canlı preview için)
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetWidgetPreviewData()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var config = await _widgetService.GetScriptConfigAsync(companyId);
        return Json(config);
    }
}

// ViewModel
public class StockReminderIndexViewModel
{
    public StockReminderSettingsDto Settings { get; set; } = new();
    public StockReminderStatsDto Stats { get; set; } = new();
    public string? SiteUrl { get; set; }
}

// DTO for stock alarm products
public class StokAlarmUrunDto
{
    public int StokAlarmUrunID { get; set; }
    public int UrunKartiID { get; set; }
    public string? UrunAdi { get; set; }
    public string? StokKodu { get; set; }
    public int ToplamStokAdedi { get; set; }
    public double UrunFiyati { get; set; }
    public double UrunFiyatiKdv { get; set; }
    public string? ParaBirimi { get; set; }
    public string? ResimUrl { get; set; }
    public string? UrunUrl { get; set; }
    public int VaryasyonSayisi { get; set; }
    public int UyeID { get; set; }
    public DateTime EklemeTarihi { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
}

// Request DTOs
public class SendStockNotificationRequest
{
    public List<int> ProductIds { get; set; } = new();
    public int? CurrentStock { get; set; }
}

public class SendStockArrivedNotificationRequest
{
    public List<StockArrivedProductDto> Products { get; set; } = new();
}

public class StockArrivedProductDto
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductUrl { get; set; }
    public int CurrentStock { get; set; }
}

/// <summary>
/// Stok hatırlatma widget ayarlarını kaydetmek için kullanılan model
/// </summary>
public class StockReminderWidgetSaveModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public string PhonePlaceholder { get; set; } = string.Empty;
    public string EmailPlaceholder { get; set; } = string.Empty;
    public string VariantOptions { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public bool ShowVariantSelector { get; set; }
    public bool ShowPhoneField { get; set; }
    public bool ShowEmailField { get; set; }
    public bool ShowNameFields { get; set; }
    public bool RequirePhone { get; set; }
    public bool ShowConsentCheckbox { get; set; }
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string TitleColor { get; set; } = string.Empty;
    public string ButtonColor { get; set; } = string.Empty;
    public string ButtonTextColor { get; set; } = string.Empty;
    public string VariantBorderColor { get; set; } = string.Empty;
    public string VariantSelectedColor { get; set; } = string.Empty;
    public string BorderRadius { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool ShowOnMobile { get; set; }
    public bool ShowOnDesktop { get; set; }
    public bool EnableAnimation { get; set; }
}

