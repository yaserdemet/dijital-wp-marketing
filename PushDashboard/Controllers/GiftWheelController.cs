using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.DTOs;
using PushDashboard.Helpers;
using PushDashboard.Services;
using PushDashboard.Services.Modules.GiftWheel;
using PushDashboard.ViewModels;
using System.Security.Claims;
using PushDashboard.Data;
using PushDashboard.Filters;
using PushDashboard.Models;
using OfficeOpenXml;
using System.Text;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace PushDashboard.Controllers;

/// <summary>
/// Admin controller for Gift Wheel management
/// </summary>
[Authorize]
public class GiftWheelController : BaseController
{
    private readonly IGiftWheelService _giftWheelService;
    private readonly IGiftWheelModuleService _moduleService;
    private readonly ILogger<GiftWheelController> _logger;
    private readonly IActivityLogService _activityLogService;
    private readonly IR2StorageService _r2StorageService;

    public GiftWheelController(
        IGiftWheelService giftWheelService,
        IGiftWheelModuleService moduleService,
        ILogger<GiftWheelController> logger,
        IUserContextService userContextService,
        ApplicationDbContext context,
        IActivityLogService activityLogService,
        IR2StorageService r2StorageService) : base(userContextService, context)
    {
        _giftWheelService = giftWheelService;
        _moduleService = moduleService;
        _logger = logger;
        _activityLogService = activityLogService;
        _r2StorageService = r2StorageService;
    }

    /// <summary>
    /// Main gift wheel management page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Check if company has active module
        if (!await _moduleService.HasActiveModuleAsync(companyId.Value))
        {
            ViewBag.ErrorMessage = "Hediye çarkı modülü aktif değil. Lütfen modülü satın alın.";
            return View("ModuleNotActive");
        }

        // Get wheel data
        var wheel = await _giftWheelService.GetCompanyWheelAsync(companyId.Value);

        // Auto-create if not exists
        if (wheel == null)
        {
            var userId = GetCurrentUserId();
            await _giftWheelService.CreateOrUpdateWheelAsync(companyId.Value, "Hediye Çarkı", userId);
            wheel = await _giftWheelService.GetCompanyWheelAsync(companyId.Value);
        }

        var settings = wheel != null ? await _giftWheelService.GetWheelSettingsAsync(wheel.Id) : null;
        var prizes = wheel != null ? await _giftWheelService.GetWheelPrizesAsync(wheel.Id) : new List<GiftWheelPrizeDto>();
        // İstatistikler sekmesinin varsayılan filtre aralığı (Son 30 gün) ile tutarlı olsun diye
        // başlangıçta gösterilen KPI ve istatistikleri de son 30 güne göre hesapla
        var statsEndDate = DateTime.Now;
        var statsStartDate = statsEndDate.AddDays(-30);
        
        // Sayfa yüklendiğinde otomatik senkronizasyon yapıyoruz
        //await _giftWheelService.SyncOrdersFromTicimaxAsync(companyId.Value, statsStartDate, statsEndDate);

        var stats = await _giftWheelService.GetWheelStatsAsync(companyId.Value, statsStartDate, statsEndDate);
        var kpis = await _giftWheelService.GetDashboardKpisAsync(companyId.Value, statsStartDate, statsEndDate);
        var integrationStatus = await _moduleService.CheckRequiredIntegrationsAsync(companyId.Value);

        var viewModel = new GiftWheelViewModel
        {
            Wheel = wheel,
            Settings = settings,
            Prizes = prizes,
            Stats = stats,
            Kpis = kpis,
            IntegrationStatus = integrationStatus,
            CompanyId = companyId.Value
        };

        return View(viewModel);
    }

    #region Wheel Management

    /// <summary>
    /// Create or update wheel
    /// </summary>
    [HttpPost]
    [ValidateModelState]
    public async Task<IActionResult> CreateOrUpdateWheel([FromBody] CreateWheelRequest request)
    {
        var companyId = GetCurrentUserCompanyId();
        var userId = GetCurrentUserId();

        if (!companyId.HasValue || string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var (success, message) = await _giftWheelService.CreateOrUpdateWheelAsync(companyId.Value, request.Name, userId);

        // Activity Log - Create/Update Wheel
        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId.Value,
                ActivityActionTypes.Create,
                ActivityModules.GiftWheel,
                $"Hediye çarkı oluşturuldu/güncellendi: {request.Name}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success, message });
    }

    /// <summary>
    /// Toggle wheel status (active/inactive)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleWheelStatus([FromBody] ToggleWheelStatusRequest request)
    {
        var companyId = GetCurrentUserCompanyId();
        var userId = GetCurrentUserId();

        if (!companyId.HasValue || string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var (success, message) = await _giftWheelService.ToggleWheelStatusAsync(companyId.Value, request.IsActive, userId);

        // Activity Log - Toggle Wheel Status
        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId.Value,
                request.IsActive ? ActivityActionTypes.Activate : ActivityActionTypes.Deactivate,
                ActivityModules.GiftWheel,
                request.IsActive ? "Hediye çarkı aktifleştirildi" : "Hediye çarkı pasifleştirildi",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success, message });
    }

    #endregion

    #region Prize Management

    /// <summary>
    /// Get wheel prizes
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPrizes()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var wheel = await _giftWheelService.GetCompanyWheelAsync(companyId.Value);
        if (wheel == null)
        {
            return Json(ApiResponse.Error("Çark bulunamadı."));
        }

        var prizes = await _giftWheelService.GetWheelPrizesAsync(wheel.Id);

        return Json(new { success = true, data = prizes });
    }

    /// <summary>
    /// Get single prize by ID
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPrize(int id)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var wheel = await _giftWheelService.GetCompanyWheelAsync(companyId.Value);
        if (wheel == null)
        {
            return Json(ApiResponse.Error("Çark bulunamadı."));
        }

        var prizes = await _giftWheelService.GetWheelPrizesAsync(wheel.Id);
        var prize = prizes.FirstOrDefault(p => p.Id == id);

        if (prize == null)
        {
            return Json(ApiResponse.Error("Ödül bulunamadı."));
        }

        return Json(new { success = true, data = prize });
    }

    /// <summary>
    /// Add new prize
    /// </summary>
    [HttpPost]
    [ValidateModelState]
    public async Task<IActionResult> AddPrize([FromBody] GiftWheelPrizeRequest request)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var wheel = await _giftWheelService.GetCompanyWheelAsync(companyId.Value);
        if (wheel == null)
        {
            return Json(ApiResponse.Error("Çark bulunamadı."));
        }

        var (success, message, prize) = await _giftWheelService.AddPrizeAsync(wheel.Id, request);

        // Auto-export to R2 on successful prize addition
        if (success)
        {
            try
            {
                await _giftWheelService.ExportGiftWheelToR2Async(companyId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-export to R2 after adding prize");
            }

            // Activity Log - Add Prize
            await _activityLogService.LogActivityAsync(
                GetCurrentUserId()!,
                companyId.Value,
                ActivityActionTypes.Create,
                ActivityModules.GiftWheel,
                $"Yeni ödül eklendi: {request.Name}",
                entityId: prize?.Id.ToString(),
                entityType: "GiftWheelPrize",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success, message, data = prize });
    }

    /// <summary>
    /// Update existing prize
    /// </summary>
    [HttpPut]
    [ValidateModelState]
    public async Task<IActionResult> UpdatePrize(int id, [FromBody] GiftWheelPrizeRequest request)
    {
        var (success, message) = await _giftWheelService.UpdatePrizeAsync(id, request);

        // Auto-export to R2 on successful prize update
        if (success)
        {
            try
            {
                var companyId = GetCurrentUserCompanyId();
                if (companyId.HasValue)
                {
                    await _giftWheelService.ExportGiftWheelToR2Async(companyId.Value);

                    // Activity Log - Update Prize
                    var userId = GetCurrentUserId();
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await _activityLogService.LogActivityAsync(
                            userId,
                            companyId.Value,
                            ActivityActionTypes.Update,
                            ActivityModules.GiftWheel,
                            $"Ödül güncellendi: {request.Name}",
                            entityId: id.ToString(),
                            entityType: "GiftWheelPrize",
                            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                            userAgent: Request.Headers["User-Agent"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-export to R2 after updating prize");
            }
        }

        return Json(new { success, message });
    }

    /// <summary>
    /// Delete prize
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeletePrize(int id)
    {
        var (success, message) = await _giftWheelService.DeletePrizeAsync(id);

        if (success)
        {
            var companyId = GetCurrentUserCompanyId();
            var userId = GetCurrentUserId();
            if (companyId.HasValue && !string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.Delete,
                    ActivityModules.GiftWheel,
                    $"Ödül silindi (ID: {id})",
                    entityId: id.ToString(),
                    entityType: "GiftWheelPrize",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
        }

        return Json(new { success, message });
    }

    /// <summary>
    /// Delete all prizes
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAllPrizes()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var wheel = await _giftWheelService.GetCompanyWheelAsync(companyId.Value);
        if (wheel == null)
        {
            return Json(ApiResponse.Error("Çark bulunamadı."));
        }

        var (success, message) = await _giftWheelService.DeleteAllPrizesAsync(wheel.Id);

        if (success)
        {
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.Delete,
                    ActivityModules.GiftWheel,
                    "Tüm ödüller silindi",
                    entityId: wheel.Id.ToString(),
                    entityType: "GiftWheel",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
        }

        return Json(new { success, message });
    }

    /// <summary>
    /// Toggle prize status
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> TogglePrizeStatus(int id, [FromBody] TogglePrizeStatusRequest request)
    {
        var (success, message) = await _giftWheelService.TogglePrizeStatusAsync(id, request.IsActive);

        if (success)
        {
            var companyId = GetCurrentUserCompanyId();
            var userId = GetCurrentUserId();
            if (companyId.HasValue && !string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.StatusChange,
                    ActivityModules.GiftWheel,
                    $"Ödül durumu {(request.IsActive ? "aktif" : "pasif")} yapıldı (ID: {id})",
                    entityId: id.ToString(),
                    entityType: "GiftWheelPrize",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
        }

        return Json(new { success, message });
    }

    /// <summary>
    /// Toggle all prizes status
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleAllPrizes([FromBody] ToggleAllPrizesRequest request)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var wheel = await _giftWheelService.GetCompanyWheelAsync(companyId.Value);
        if (wheel == null)
        {
            return Json(ApiResponse.Error("Çark bulunamadı."));
        }

        var (success, message) = await _giftWheelService.ToggleAllPrizesAsync(wheel.Id, request.IsActive);

        if (success)
        {
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.StatusChange,
                    ActivityModules.GiftWheel,
                    $"Tüm ödüller {(request.IsActive ? "aktif" : "pasif")} yapıldı",
                    entityType: "GiftWheelPrize",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
        }

        return Json(new { success, message });
    }

    /// <summary>
    /// Reorder prizes
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReorderPrizes([FromBody] ReorderPrizesRequest request)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var wheel = await _giftWheelService.GetCompanyWheelAsync(companyId.Value);
        if (wheel == null)
        {
            return Json(ApiResponse.Error("Çark bulunamadı."));
        }

        var (success, message) = await _giftWheelService.ReorderPrizesAsync(wheel.Id, request.PrizeOrders);

        return Json(new { success, message });
    }

    #endregion

    #region Settings Management

    /// <summary>
    /// Get wheel settings
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var wheel = await _giftWheelService.GetCompanyWheelAsync(companyId.Value);
        if (wheel == null)
        {
            return Json(ApiResponse.Error("Çark bulunamadı."));
        }

        var settings = await _giftWheelService.GetWheelSettingsAsync(wheel.Id);

        return Json(new { success = true, data = settings });
    }

    /// <summary>
    /// Update wheel settings
    /// </summary>
    [HttpPost]
    [ValidateModelState]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateGiftWheelSettingsRequest request)
    {
        var companyId = GetCurrentUserCompanyId();
        var userId = GetCurrentUserId();

        if (!companyId.HasValue || string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var wheel = await _giftWheelService.GetCompanyWheelAsync(companyId.Value);
        if (wheel == null)
        {
            return Json(ApiResponse.Error("Çark bulunamadı."));
        }

        var (success, message) = await _giftWheelService.UpdateWheelSettingsAsync(wheel.Id, request, userId);

        if (success)
        {
            try
            {
                await _giftWheelService.ExportGiftWheelToR2Async(companyId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-export to R2 after updating settings");
            }

            await _activityLogService.LogActivityAsync(
                userId,
                companyId.Value,
                ActivityActionTypes.Settings,
                ActivityModules.GiftWheel,
                "Hediye çarkı ayarları güncellendi",
                entityId: wheel.Id.ToString(),
                entityType: "GiftWheel",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success, message });
    }

    /// <summary>
    /// Upload wheel logo
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        if (file == null || file.Length == 0)
        {
            return Json(ApiResponse.Error("Dosya seçilmedi."));
        }

        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            return Json(ApiResponse.Error("Dosya boyutu 5MB'dan küçük olmalıdır."));
        }

        // Validate file type
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return Json(ApiResponse.Error("Sadece PNG, JPG, JPEG ve SVG dosyaları yüklenebilir."));
        }

        try
        {
            var fileName = $"logo{extension}";
            var r2Key = $"giftwheel/{companyId.Value}/{fileName}";

            var contentType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            string logoUrl;
            using (var stream = file.OpenReadStream())
            {
                logoUrl = await _r2StorageService.UploadFileAsync(r2Key, stream, contentType);
            }

            var returnUrl = $"{logoUrl}{(logoUrl.Contains("?") ? "&" : "?")}v={DateTime.Now.Ticks}";

            return Json(new { success = true, url = returnUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo for company {CompanyId}", companyId);
            return Json(ApiResponse.Error("Dosya yüklenirken bir hata oluştu."));
        }
    }

    /// <summary>
    /// Upload wheel background image
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(new { success = false, message = "Oturum bilgisi bulunamadı." });
        }

        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Dosya seçilmedi." });
        }

        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            return Json(new { success = false, message = "Dosya boyutu 5MB'dan küçük olmalıdır." });
        }

        // Validate file type
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return Json(new { success = false, message = "Sadece PNG, JPG, JPEG, WEBP ve SVG dosyaları yüklenebilir." });
        }

        try
        {
            var fileName = $"bg-{Guid.NewGuid()}{extension}";
            var r2Key = $"giftwheel/{companyId.Value}/{fileName}";

            var contentType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            string imageUrl;
            using (var stream = file.OpenReadStream())
            {
                imageUrl = await _r2StorageService.UploadFileAsync(r2Key, stream, contentType);
            }

            var returnUrl = $"{imageUrl}{(imageUrl.Contains("?") ? "&" : "?")}v={DateTime.Now.Ticks}";

            return Json(new { success = true, url = returnUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading background image for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Dosya yüklenirken bir hata oluştu." });
        }
    }

    #endregion

    #region Statistics and Reporting

    /// <summary>
    /// Get wheel statistics
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStats(DateTime? startDate = null, DateTime? endDate = null)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var stats = await _giftWheelService.GetWheelStatsAsync(companyId.Value, startDate, endDate);

        return Json(new { success = true, data = stats });
    }

    /// <summary>
    /// Get recent spins
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRecentSpins(int limit = 50)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var recentSpins = await _giftWheelService.GetRecentSpinsAsync(companyId.Value, limit);

        return Json(new { success = true, data = recentSpins });
    }

    /// <summary>
    /// Get daily statistics for charts
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDailyStats(int days = 30)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var dailyStats = await _giftWheelService.GetDailyStatsAsync(companyId.Value, days);

        return Json(new { success = true, data = dailyStats });
    }

    /// <summary>
    /// Get dashboard KPI data
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboardKpis(DateTime? startDate = null, DateTime? endDate = null)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        // İstatistikler istendiğinde otomatik senkronizasyon yap
        //await _giftWheelService.SyncOrdersFromTicimaxAsync(companyId.Value, startDate, endDate);

        var kpis = await _giftWheelService.GetDashboardKpisAsync(companyId.Value, startDate, endDate);

        return Json(new { success = true, data = kpis });
    }

    /// <summary>
    /// Sync orders from Ticimax and update voucher usage
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> SyncOrders(DateTime? startDate = null, DateTime? endDate = null)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }
       
        var companyJoined= await _context.Companies.Where(c => c.Id == companyId.Value).Select(c => c.CreatedAt).FirstOrDefaultAsync();
        startDate = (companyJoined != null && (startDate == null || startDate < companyJoined))
            ? companyJoined
            : startDate;
        var (success, message, updatedCount) = await _giftWheelService.SyncOrdersFromTicimaxAsync(companyId.Value, startDate, endDate);

        if (success)
        {
            return Json(new { success = true, message = message, updatedCount = updatedCount });
        }
        else
        {
            return Json(ApiResponse.Error(message));
        }
    }

    /// <summary>
    /// Get funnel statistics
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFunnelStats(DateTime? startDate = null, DateTime? endDate = null)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var funnel = await _giftWheelService.GetFunnelStatsAsync(companyId.Value, startDate, endDate);

        return Json(new { success = true, data = funnel });
    }

    /// <summary>
    /// Get trend statistics
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTrendStats(int days = 30)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var trends = await _giftWheelService.GetTrendStatsAsync(companyId.Value, days);

        return Json(new { success = true, data = trends });
    }

    /// <summary>
    /// Get detailed logs with filtering and pagination
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GetDetailedLogs([FromBody] SpinLogFilterDto filter)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        filter ??= new SpinLogFilterDto();
        var logs = await _giftWheelService.GetDetailedLogsAsync(companyId.Value, filter);



        return Json(new { success = true, data = logs });
    }

    /// <summary>
    /// Export logs to Excel
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExportExcel([FromBody] SpinLogFilterDto filter)
    {
        try 
        {
            var companyId = GetCurrentUserCompanyId();
            if (!companyId.HasValue) return Unauthorized();

            filter ??= new SpinLogFilterDto();
            filter.Page = 1;
            filter.PageSize = int.MaxValue;

            var result = await _giftWheelService.GetDetailedLogsAsync(companyId.Value, filter);
            var data = result.Items;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Rapor");

                // Title
                worksheet.Cells["A1"].Value = "Hediye Çarkı İşlem Raporu";
                worksheet.Cells["A1:H1"].Merge = true;
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells["A1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

                // Meta info
                worksheet.Cells["A2"].Value = $"Oluşturulma Tarihi: {DateTime.Now:g}";
                worksheet.Cells["A2:H2"].Merge = true;
                worksheet.Cells["A2"].Style.Font.Italic = true;

                // Headers
                var headers = new[] { "Müşteri Adı", "Telefon", "E-posta", "Ödül", "Kupon Kodu", "Kullanım Durumu", "Sipariş Tutarı", "Tarih" };
                int headerRow = 4;
                
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[headerRow, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    cell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Medium;
                }

                // Data
                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    int row = headerRow + 1 + i;
                    
                    worksheet.Cells[row, 1].Value = item.CustomerName;
                    worksheet.Cells[row, 2].Value = item.CustomerPhone;
                    worksheet.Cells[row, 3].Value = item.CustomerEmail;
                    worksheet.Cells[row, 4].Value = item.PrizeName;
                    worksheet.Cells[row, 5].Value = item.VoucherCode;
                    
                    var statusCell = worksheet.Cells[row, 6];
                    statusCell.Value = item.VoucherUsed ? "Kullanıldı" : "Kullanılmadı";
                    if (item.VoucherUsed) statusCell.Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    
                    var amountCell = worksheet.Cells[row, 7];
                    if (item.OrderAmount.HasValue)
                    {
                        amountCell.Value = item.OrderAmount;
                        amountCell.Style.Numberformat.Format = "#,##0.00 ₺";
                    }
                    else
                    {
                        amountCell.Value = "-";
                         amountCell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }

                    var dateCell = worksheet.Cells[row, 8];
                    dateCell.Value = item.SpinDate;
                    dateCell.Style.Numberformat.Format = "dd.MM.yyyy HH:mm";
                }

                worksheet.Cells.AutoFitColumns();
                
                // Add border to all cells
                var borderRange = worksheet.Cells[headerRow, 1, headerRow + data.Count, headers.Length];
                borderRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                borderRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                borderRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                borderRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                var content = package.GetAsByteArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"HediyeCarki_Rapor_{DateTime.Now:yyyyMMddHHmm}.xlsx");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting Excel");
            return BadRequest("Export failed");
        }
    }

    /// <summary>
    /// Export logs to CSV
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExportCsv([FromBody] SpinLogFilterDto filter)
    {
        try
        {
            var companyId = GetCurrentUserCompanyId();
            if (!companyId.HasValue) return Unauthorized();

            filter ??= new SpinLogFilterDto();
            filter.Page = 1;
            filter.PageSize = int.MaxValue;

            var result = await _giftWheelService.GetDetailedLogsAsync(companyId.Value, filter);
            var data = result.Items;

            var sb = new StringBuilder();
            sb.AppendLine("Müşteri Adı,Telefon,E-posta,Ödül,Kupon Kodu,Kullanım Durumu,Sipariş Tutarı,Tarih");

            foreach (var item in data)
            {
                var line = new List<string>
                {
                    EscapeCsv(item.CustomerName),
                    EscapeCsv(item.CustomerPhone),
                    EscapeCsv(item.CustomerEmail),
                    EscapeCsv(item.PrizeName),
                    EscapeCsv(item.VoucherCode),
                    item.VoucherUsed ? "Kullanıldı" : "Kullanılmadı",
                    item.OrderAmount?.ToString("F2") ?? "",
                    item.SpinDate.ToString("dd.MM.yyyy HH:mm")
                };
                sb.AppendLine(string.Join(",", line));
            }

            var content = Encoding.UTF8.GetBytes(sb.ToString());
            var preamble = Encoding.UTF8.GetPreamble();
            var resultBytes = new byte[preamble.Length + content.Length];
            Array.Copy(preamble, resultBytes, preamble.Length);
            Array.Copy(content, 0, resultBytes, preamble.Length, content.Length);

            return File(resultBytes, "text/csv", $"HediyeCarki_Rapor_{DateTime.Now:yyyyMMddHHmm}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting CSV");
            return BadRequest("Export failed");
        }
    }

    /// <summary>
    /// Export logs as Printable HTML (PDF alternative)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExportPdf([FromBody] SpinLogFilterDto filter)
    {
        try
        {
            var companyId = GetCurrentUserCompanyId();
            if (!companyId.HasValue) return Unauthorized();

            filter ??= new SpinLogFilterDto();
            filter.Page = 1;
            filter.PageSize = int.MaxValue;

            var result = await _giftWheelService.GetDetailedLogsAsync(companyId.Value, filter);
            var data = result.Items;

            // Calculate summaries for the report
            var totalRevenue = data.Sum(x => x.OrderAmount ?? 0);
            var usedVouchers = data.Count(x => x.VoucherUsed);
            var successRate = data.Count > 0 ? (double)usedVouchers / data.Count * 100 : 0;

            var sb = new StringBuilder();
            sb.Append(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Hediye Çarkı Performans Raporu</title>
    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap' rel='stylesheet'>
    <style>
        :root {
            --primary: #4f46e5;
            --primary-dark: #4338ca;
            --secondary: #64748b;
            --success: #10b981;
            --success-bg: #d1fae5;
            --success-text: #065f46;
            --neutral-bg: #f9fafb;
            --border: #e2e8f0;
            --text-main: #1f2937;
            --text-light: #6b7280;
        }
        body { 
            font-family: 'Inter', system-ui, -apple-system, sans-serif; 
            margin: 0; 
            padding: 40px; 
            color: var(--text-main); 
            background-color: #fff;
            line-height: 1.5;
        }
        .container { 
            max-width: 100%; 
            margin: 0 auto; 
        }
        
        /* Header Section */
        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 40px;
            padding-bottom: 20px;
            border-bottom: 2px solid var(--border);
        }
        .brand {
            font-size: 24px;
            font-weight: 800;
            color: var(--primary);
            letter-spacing: -0.5px;
        }
        .report-title {
            text-align: right;
        }
        .report-title h1 {
            margin: 0;
            font-size: 28px;
            font-weight: 700;
            color: var(--text-main);
        }
        .report-meta {
            color: var(--text-light);
            font-size: 14px;
            margin-top: 5px;
        }

        /* Summary Cards */
        .summary-grid {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 20px;
            margin-bottom: 40px;
        }
        .card {
            background: #fff;
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 20px;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
        }
        .card-title {
            font-size: 13px;
            font-weight: 600;
            text-transform: uppercase;
            color: var(--text-light);
            letter-spacing: 0.05em;
            margin-bottom: 8px;
        }
        .card-value {
            font-size: 24px;
            font-weight: 700;
            color: var(--text-main);
        }

        /* Table Styling */
        table { 
            width: 100%; 
            border-collapse: separate; 
            border-spacing: 0; 
            margin-top: 10px; 
            font-size: 13px;
        }
        th { 
            background-color: var(--neutral-bg); 
            color: var(--text-light); 
            font-weight: 600; 
            text-align: left; 
            padding: 12px 16px;
            text-transform: uppercase;
            font-size: 11px;
            letter-spacing: 0.05em;
            border-top: 1px solid var(--border);
            border-bottom: 1px solid var(--border);
        }
        td { 
            padding: 16px; 
            border-bottom: 1px solid var(--border);
            vertical-align: middle;
        }
        tr:last-child td {
            border-bottom: none;
        }
        
        /* Badges & Text Styles */
        .badge {
            display: inline-flex;
            align-items: center;
            padding: 2px 10px;
            border-radius: 9999px;
            font-size: 11px;
            font-weight: 600;
        }
        .badge-success { background-color: var(--success-bg); color: var(--success-text); }
        .badge-neutral { background-color: #f3f4f6; color: #4b5563; }
        
        .code-block {
            font-family: 'Monaco', 'Consolas', monospace;
            background: #eff6ff;
            color: #1e40af;
            padding: 4px 8px;
            border-radius: 4px;
            font-size: 12px;
        }
        
        .customer-name { font-weight: 600; color: #111827; }
        .customer-contact { font-size: 12px; color: var(--text-light); display: block; margin-top: 2px; }
        .amount { font-family: 'Inter', sans-serif; font-weight: 600; }

        /* Footer */
        .footer { 
            margin-top: 60px; 
            padding-top: 20px; 
            border-top: 1px solid var(--border);
            display: flex;
            justify-content: space-between;
            align-items: center;
            font-size: 12px;
            color: var(--text-light);
        }

        @media print {
            body { padding: 0; }
            .header { margin-bottom: 30px; }
            .card { break-inside: avoid; border: 1px solid #ccc; box-shadow: none; }
            th { background-color: #f8f9fa !important; -webkit-print-color-adjust: exact; }
            .badge-success { background-color: #d1fae5 !important; -webkit-print-color-adjust: exact; }
            .code-block { border: 1px solid #eee; }
        }
    </style>
</head>
<body>
    <div class='container'>
        <!-- Header -->
        <div class='header'>
            <div class='brand'>DIJITALDESIN</div>
            <div class='report-title'>
                <h1>Hediye Çarkı Raporu</h1>
                <div class='report-meta'>
                    Oluşturulma: " + DateTime.Now.ToString("dd MMMM yyyy HH:mm") + @"
                </div>
            </div>
        </div>

        <!-- KPI Cards -->
        <div class='summary-grid'>
            <div class='card'>
                <div class='card-title'>Toplam Kayıt</div>
                <div class='card-value'>" + data.Count + @"</div>
            </div>
            <div class='card'>
                <div class='card-title'>Kupon Kullanımı</div>
                <div class='card-value'>" + usedVouchers + @"</div>
            </div>
             <div class='card'>
                <div class='card-title'>Kullanım Oranı</div>
                <div class='card-value'>" + successRate.ToString("F1") + @"%</div>
            </div>
            <div class='card'>
                <div class='card-title'>Toplam Ciro</div>
                <div class='card-value'>" + totalRevenue.ToString("N2") + @" ₺</div>
            </div>
        </div>

        <!-- Data Table -->
        <table>
            <thead>
                <tr>
                    <th style='width: 25%'>Müşteri Bilgileri</th>
                    <th style='width: 20%'>Kazanılan Ödül</th>
                    <th style='width: 15%'>Kupon Kodu</th>
                    <th style='width: 15%'>Durum</th>
                    <th style='width: 10%'>Sipariş</th>
                    <th style='width: 15%; text-align: right'>Tarih</th>
                </tr>
            </thead>
            <tbody>");

            foreach (var item in data)
            {
                var amount = item.OrderAmount.HasValue ? string.Format("{0:N2} ₺", item.OrderAmount) : "-";
                
                sb.Append($@"
            <tr>
                <td>
                    <div class='customer-name'>{System.Net.WebUtility.HtmlEncode(item.CustomerName)}</div>
                    <span class='customer-contact'>{System.Net.WebUtility.HtmlEncode(item.CustomerPhone)}</span>
                    <span class='customer-contact'>{System.Net.WebUtility.HtmlEncode(item.CustomerEmail)}</span>
                </td>
                <td>
                    <div style='font-weight: 500;'>{System.Net.WebUtility.HtmlEncode(item.PrizeName)}</div>
                </td>
                <td>
                    <span class='code-block'>{System.Net.WebUtility.HtmlEncode(item.VoucherCode)}</span>
                </td>
                <td>");
                
                if (item.VoucherUsed)
                {
                    sb.Append("<span class='badge badge-success'>Kullanıldı</span>");
                }
                else
                {
                    sb.Append("<span class='badge badge-neutral'>Bekliyor</span>");
                }

                sb.Append($@"</td>
                <td class='amount'>{amount}</td>
                <td style='text-align: right; color: var(--secondary);'>
                    {item.SpinDate:dd.MM.yyyy}<br>
                    <small>{item.SpinDate:HH:mm}</small>
                </td>
            </tr>");
            }

            sb.Append(@"
            </tbody>
        </table>

        <!-- Footer -->
        <div class='footer'>
            <div>Gizli ve Özeldir - Sadece şirket içi kullanım içindir.</div>
            <div>Sayfa 1 / 1</div>
        </div>
    </div>
</body>
</html>");

            var htmlContent = sb.ToString();
            
            // If requested via AJAX for client-side PDF generation
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.ContentType?.Contains("application/json") == true)
            {
                return Json(new { success = true, data = htmlContent });
            }

            var content = Encoding.UTF8.GetBytes(htmlContent);
            return File(content, "text/html", $"HediyeCarki_Rapor_{DateTime.Now:yyyyMMddHHmm}.html");
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error exporting PDF/HTML");
            return BadRequest("Export failed");
        }
    }

    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }



    #endregion

    #region Script Generation

    /// <summary>
    /// Get JavaScript embed script
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEmbedScript()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Content("// Oturum bilgisi bulunamadı.", "application/javascript");
        }

        var script = await _giftWheelService.GenerateEmbedScriptAsync(companyId.Value);

        return Content(script, "application/javascript");
    }

    /// <summary>
    /// Get wheel configuration for preview
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWheelConfig()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var config = await _giftWheelService.GetWheelConfigAsync(companyId.Value);

        return Json(new { success = true, data = config });
    }

    #endregion

    #region Module Management

    /// <summary>
    /// Get module settings
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetModuleSettings()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var settings = await _moduleService.GetModuleSettingsAsync(companyId.Value);

        return Json(new { success = true, data = settings });
    }

    /// <summary>
    /// Update module settings
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateModuleSettings([FromBody] GiftWheelModuleSettingsDto settings)
    {
        var companyId = GetCurrentUserCompanyId();
        var userId = GetCurrentUserId();

        if (!companyId.HasValue || string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var (success, message) = await _moduleService.UpdateModuleSettingsAsync(companyId.Value, settings, userId);

        return Json(new { success, message });
    }

    /// <summary>
    /// Get integration status
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetIntegrationStatus()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var status = await _moduleService.CheckRequiredIntegrationsAsync(companyId.Value);

        return Json(new { success = true, data = status });
    }

    #endregion

    #region R2 Export

    /// <summary>
    /// Export gift wheel configuration to R2 storage
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExportToR2()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _giftWheelService.ExportGiftWheelToR2Async(companyId.Value);

        if (result.Contains("successfully"))
        {
            TempData["SuccessMessage"] = "Hediye çarkı başarıyla R2'ye aktarıldı.";
        }
        else
        {
            TempData["ErrorMessage"] = $"R2 aktarımı başarısız: {result}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Export gift wheel configuration to R2 storage (AJAX version)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExportToR2Ajax()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var result = await _giftWheelService.ExportGiftWheelToR2Async(companyId.Value);

        if (result.Contains("successfully"))
        {
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.Export,
                    ActivityModules.GiftWheel,
                    "Hediye çarkı siteye aktarıldı",
                    entityType: "GiftWheel",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }

            return Json(ApiResponse.SuccessMessage("Hediye çarkı başarıyla R2'ye aktarıldı."));
        }
        else
        {
            return Json(new { success = false, message = $"R2 aktarımı başarısız: {result}" });
        }
    }

    #endregion
}
