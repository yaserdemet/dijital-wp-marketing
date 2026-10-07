using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Models.ViewModels;
using PushDashboard.Services;
using System.Text.Json;
using PushDashboard.Data;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class CookieManagementController : BaseController
{
    private readonly ICookieManagementService _cookieManagementService;
    private readonly ILogger<CookieManagementController> _logger;
    private readonly IActivityLogService _activityLogService;

    public CookieManagementController(
        ICookieManagementService cookieManagementService,
        ILogger<CookieManagementController> logger,
        IActivityLogService activityLogService,
        ApplicationDbContext context,
        IUserContextService userContextService) : base(userContextService,context)
    {
        _cookieManagementService = cookieManagementService;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Modül kontrolü
        var hasModule = await _cookieManagementService.HasCookieManagementModuleAsync(companyId.Value);
        if (!hasModule)
        {
            TempData["ErrorMessage"] = "Çerez Yönetimi modülüne sahip değilsiniz. Lütfen önce modülü satın alın.";
            return RedirectToAction("Index", "Store");
        }

        // Mevcut ayarları getir veya varsayılan oluştur
        var cookieManagement = await _cookieManagementService.GetCookieManagementAsync(companyId.Value);
        if (cookieManagement == null)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            cookieManagement = await _cookieManagementService.CreateDefaultSettingsAsync(companyId.Value, userId);
        }

        // İstatistikleri getir
        var stats = await _cookieManagementService.GetCookieStatsAsync(companyId.Value);

        var viewModel = new CookieManagementIndexViewModel
        {
            Settings = cookieManagement,
            Stats = stats
        };

        ViewData["Title"] = "Çerez Yönetimi";
        
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings([FromBody] CookieManagementSaveModel model)
    {
        var companyId = GetCurrentUserCompanyId();
        var userId = GetCurrentUserId();

        if (!companyId.HasValue || string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Kullanıcı bilgileri bulunamadı."));
        }

        // Modül kontrolü
        var hasModule = await _cookieManagementService.HasCookieManagementModuleAsync(companyId.Value);
        if (!hasModule)
        {
            return Json(ApiResponse.Error("Çerez Yönetimi modülüne sahip değilsiniz."));
        }

        var cookieManagement = new CookieManagement
        {
            CompanyId = companyId.Value,
            BannerTitle = model.BannerTitle,
            BannerDescription = model.BannerDescription,
            AcceptButtonText = model.AcceptButtonText,
            RejectButtonText = model.RejectButtonText,
            SettingsButtonText = model.SettingsButtonText,
            SaveButtonText = model.SaveButtonText,
            BannerPosition = model.BannerPosition,
            BannerBackgroundColor = model.BannerBackgroundColor,
            BannerTextColor = model.BannerTextColor,
            AcceptButtonColor = model.AcceptButtonColor,
            RejectButtonColor = model.RejectButtonColor,
            SettingsButtonColor = model.SettingsButtonColor,
            BorderRadius = model.BorderRadius,
            ShowSettingsButton = model.ShowSettingsButton,
            EnableAnimation = model.EnableAnimation,
            IsActive = true, // Modüle sahipse her zaman aktif
            CookieExpiryDays = model.CookieExpiryDays,
            UpdatedByUserId = userId
        };

        // Kategorileri ayarla
        cookieManagement.Categories = model.Categories ?? new List<CookieCategory>();

        var success = await _cookieManagementService.SaveCookieManagementAsync(cookieManagement);

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId.Value,
                ActivityActionTypes.Update,
                ActivityModules.CookieManagement,
                "Çerez yönetimi ayarları güncellendi",
                entityType: "CookieManagement",
                details: new { model.BannerTitle, model.BannerPosition, CategoriesCount = model.Categories?.Count ?? 0 },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SuccessMessage("Ayarlar başarıyla kaydedildi."));
        }
        else
        {
            return Json(ApiResponse.Error("Ayarlar kaydedilirken bir hata oluştu."));
        }
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetPreviewData()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var config = await _cookieManagementService.GetScriptConfigAsync(companyId);
        return Json(new { success = true, data = config });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportToR2()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(new { success = false, message = "Şirket bilgisi bulunamadı." });
        }

        var jsonUrl = await _cookieManagementService.ExportCookieManagementToR2Async(companyId.Value);

        _logger.LogInformation("Cookie management exported to R2 for company {CompanyId}", companyId);

        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId.Value,
                ActivityActionTypes.Export,
                ActivityModules.CookieManagement,
                "Çerez yönetimi siteye aktarıldı",
                entityType: "CookieManagement",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success = true, message = "Siteye başarıyla aktarıldı!", jsonUrl = jsonUrl });
    }
}

/// <summary>
/// Çerez yönetimi ayarlarını kaydetmek için kullanılan model
/// </summary>
public class CookieManagementSaveModel
{
    public string BannerTitle { get; set; } = string.Empty;
    public string BannerDescription { get; set; } = string.Empty;
    public string AcceptButtonText { get; set; } = string.Empty;
    public string RejectButtonText { get; set; } = string.Empty;
    public string SettingsButtonText { get; set; } = string.Empty;
    public string SaveButtonText { get; set; } = string.Empty;
    public string BannerPosition { get; set; } = string.Empty;
    public string BannerBackgroundColor { get; set; } = string.Empty;
    public string BannerTextColor { get; set; } = string.Empty;
    public string AcceptButtonColor { get; set; } = string.Empty;
    public string RejectButtonColor { get; set; } = string.Empty;
    public string SettingsButtonColor { get; set; } = string.Empty;
    public string BorderRadius { get; set; } = string.Empty;
    public bool ShowSettingsButton { get; set; }
    public bool EnableAnimation { get; set; }
    public int CookieExpiryDays { get; set; }
    public List<CookieCategory>? Categories { get; set; }
}
