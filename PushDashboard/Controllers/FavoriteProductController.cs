using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Filters;
using PushDashboard.Services;
using PushDashboard.Services.Integrations.Ticimax;
using PushDashboard.Services.Modules.FavoriteProduct;

namespace PushDashboard.Controllers;

[Authorize]
public class FavoriteProductController : BaseController
{
    private readonly IFavoriteProductService _favoriteProductService;
    private readonly ILogger<FavoriteProductController> _logger;
    private readonly ApplicationDbContext _dbContext;

    public FavoriteProductController(
        IFavoriteProductService favoriteProductService,
        ILogger<FavoriteProductController> logger,
        ApplicationDbContext context,
        IUserContextService userContextService) : base(userContextService, context)
    {
        _favoriteProductService = favoriteProductService;
        _logger = logger;
        _dbContext = context;
    }

    /// <summary>
    /// Favori ürünler ana sayfa
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
            // Ticimax entegrasyonu kontrol et
            var integration = await _dbContext.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId.Value
                    && ci.Integration.Name == "Ticimax"
                    && ci.IsActive
                    && ci.IsConfigured);
            string? siteUrl = null;
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

            // Son senkronizasyon bilgisi
            var lastSync = await _favoriteProductService.GetLastSyncAsync(companyId.Value);

            // Veritabanında veri var mı?
            var hasData = await _dbContext.FavoriteProducts
                .AnyAsync(f => f.CompanyId == companyId.Value && f.IsActive);

            // Modül ayarlarını al
            var settings = await _favoriteProductService.GetModuleSettingsAsync(companyId.Value);

            var stats = await _favoriteProductService.GetStatsAsync(companyId.Value);

            var viewModel = new FavoriteProductIndexViewModel
            {
                HasIntegration = integration != null,
                IntegrationPlatform = integration?.Integration.Name ?? "",
                HasData = hasData,
                LastSyncAt = lastSync?.CompletedAt,
                LastSyncSuccess = lastSync?.IsSuccess ?? false,
                LastSyncNewRecords = lastSync?.NewRecords ?? 0,
                LastSyncUpdatedRecords = lastSync?.UpdatedRecords ?? 0,
                LastSyncDeletedRecords = lastSync?.DeletedRecords ?? 0,
                SiteUrl = siteUrl,
                Settings = settings,
                Stats = new FavoriteProductStatsDto
                {
                    TotalFavoriteProducts = stats.TotalFavoriteProducts,
                    TotalFavoriteCount = stats.TotalFavoriteCount,
                    TotalUniqueMembers = stats.TotalUniqueMembers,
                    FavoritesThisMonth = stats.FavoritesThisMonth,
                    FavoritesThisWeek = stats.FavoritesThisWeek
                }
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading favorite products page for company {CompanyId}", companyId);
            TempData["ErrorMessage"] = "Sayfa yüklenirken bir hata oluştu.";
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Favori ürünleri Ticimax'tan çekip veritabanına kaydeder
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> SyncFavorites([FromQuery] bool sendNotifications = false)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId() ?? "";

        try
        {
            var result = await _favoriteProductService.SyncFavoriteProductsAsync(companyId, userId, sendNotifications);
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing favorite products for company {CompanyId}", companyId);
            return Json(new FavoriteProductSyncResult
            {
                Success = false,
                Message = "Senkronizasyon sırasında hata oluştu: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Veritabanından favori ürünleri getirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetFavoriteProducts(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var result = await _favoriteProductService.GetFavoriteProductsAsync(
                companyId, page, pageSize, searchTerm, startDate, endDate);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                data = result.Products,
                totalCount = result.TotalCount,
                totalFavoriteCount = result.TotalFavoriteCount,
                totalPages = result.TotalPages,
                currentPage = result.CurrentPage,
                hasMore = result.CurrentPage < result.TotalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite products for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Favori ürünler alınırken hata oluştu." });
        }
    }

    /// <summary>
    /// Belirli bir ürünü favorilerine ekleyen üyeleri getirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetProductFavoriteMembers(int productId, int page = 1, int pageSize = 20)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var members = await _favoriteProductService.GetProductFavoriteMembersAsync(
                companyId, productId, page, pageSize);

            // Total count için ayrı sorgu
            var totalCount = await _dbContext.FavoriteProducts
                .CountAsync(f => f.CompanyId == companyId &&
                                f.ExternalProductId == productId &&
                                f.IsActive);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return Json(new
            {
                success = true,
                data = members,
                totalCount,
                totalPages,
                currentPage = page
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product favorite members for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Üye listesi alınırken hata oluştu." });
        }
    }
    
    /// <summary>
    /// Modül ayarlarını getirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetSettings()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var settings = await _favoriteProductService.GetModuleSettingsAsync(companyId);
            return Json(new { success = true, settings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite product settings for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Ayarlar yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Modül ayarlarını günceller
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> UpdateSettings([FromBody] DTOs.UpdateFavoriteProductSettingsRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var success = await _favoriteProductService.UpdateModuleSettingsAsync(companyId, request);
            return Json(new { success, message = success ? "Ayarlar güncellendi." : "Ayarlar güncellenirken hata oluştu." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating favorite product settings for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Ayarlar güncellenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Favori ürün istatistiklerini getirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetStats()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var stats = await _favoriteProductService.GetStatsAsync(companyId);

            return Json(new
            {
                success = true,
                stats = new
                {
                    totalFavoriteProducts = stats.TotalFavoriteProducts,
                    totalFavoriteCount = stats.TotalFavoriteCount,
                    totalUniqueMembers = stats.TotalUniqueMembers,
                    favoritesThisMonth = stats.FavoritesThisMonth,
                    favoritesThisWeek = stats.FavoritesThisWeek,
                    favoritesToday = stats.FavoritesToday,
                    lastSyncAt = stats.LastSyncAt,
                    hasData = stats.HasData
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite product stats for company {CompanyId}", companyId);
            return Json(new
            {
                success = true,
                stats = new FavoriteProductStatsDto()
            });
        }
    }

    /// <summary>
    /// Belirli bir ürünün favori sahibi müşterilerine bildirim gönderir
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> SendNotification([FromBody] DTOs.SendFavoriteNotificationRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId() ?? "";

        try
        {
            var result = await _favoriteProductService.SendNotificationAsync(
                companyId, request.ProductId, request.NotificationType, request.CustomMessage ?? "", userId);

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for product {ProductId}", request.ProductId);
            return Json(new FavoriteProductNotificationResult
            {
                Success = false,
                Message = "Bildirim gönderilirken hata oluştu: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Seçili ürünlerin favori sahibi müşterilerine toplu bildirim gönderir
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> SendBulkNotification([FromBody] DTOs.SendBulkFavoriteNotificationRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId() ?? "";

        if (request.ProductIds == null || !request.ProductIds.Any())
        {
            return Json(new { success = false, message = "Lütfen en az bir ürün seçiniz." });
        }

        try
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var productId in request.ProductIds)
            {
                try 
                {
                    var result = await _favoriteProductService.SendNotificationAsync(
                        companyId, productId, request.NotificationType, request.CustomMessage ?? "", userId);
                    
                    if (result.Success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (Exception pEx)
                {
                    _logger.LogError(pEx, "Error sending bulk notification for product {ProductId}", productId);
                    failCount++;
                }
            }

            return Json(new { 
                success = true, 
                message = $"{successCount} ürün için bildirimler başarıyla gönderildi." + (failCount > 0 ? $" ({failCount} hata)" : ""),
                notificationsSent = successCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk notifications for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Bildirimler gönderilirken genel bir hata oluştu." });
        }
    }

    /// <summary>
    /// Son senkronizasyon bilgisini getirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetLastSync()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var lastSync = await _favoriteProductService.GetLastSyncAsync(companyId);

            if (lastSync == null)
            {
                return Json(new { success = true, hasSync = false });
            }

            return Json(new
            {
                success = true,
                hasSync = true,
                syncedAt = lastSync.CompletedAt,
                isSuccess = lastSync.IsSuccess,
                totalFetched = lastSync.TotalFetched,
                newRecords = lastSync.NewRecords,
                updatedRecords = lastSync.UpdatedRecords,
                deletedRecords = lastSync.DeletedRecords
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last sync for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Son senkronizasyon bilgisi alınamadı." });
        }
    }
}

// ViewModels & DTOs
public class FavoriteProductIndexViewModel
{
    public bool HasIntegration { get; set; }
    public string IntegrationPlatform { get; set; } = "";
    public bool HasData { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool LastSyncSuccess { get; set; }
    public int LastSyncNewRecords { get; set; }
    public int LastSyncUpdatedRecords { get; set; }
    public int LastSyncDeletedRecords { get; set; }
    public string? SiteUrl { get; set; }
    public PushDashboard.DTOs.FavoriteProductSettingsDto Settings { get; set; } = new();
    public FavoriteProductStatsDto Stats { get; set; } = new();
}

public class FavoriUrunDto
{
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
    public int FavoriSayisi { get; set; }
    public DateTime SonEklemeTarihi { get; set; }
    public DateTime IlkEklemeTarihi { get; set; }
}

public class FavoriteProductStatsDto
{
    public int TotalFavoriteProducts { get; set; }
    public int TotalFavoriteCount { get; set; }
    public int TotalUniqueMembers { get; set; }
    public int FavoritesThisMonth { get; set; }
    public int FavoritesThisWeek { get; set; }
}

public class FavoriUyeDto
{
    public int UyeID { get; set; }
    public string UyeAdi { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime EklemeTarihi { get; set; }
    public int FavoriUrunID { get; set; }
    public string? CustomerPageUrl { get; set; }
}
