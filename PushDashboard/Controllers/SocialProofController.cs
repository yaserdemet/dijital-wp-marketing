using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.ViewModels.SocialProof;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class SocialProofController : BaseController
{
    private readonly ISocialProofService _socialProofService;
    private readonly ILogger<SocialProofController> _logger;
    private readonly IActivityLogService _activityLogService;

    public SocialProofController(
        ISocialProofService socialProofService,
        ILogger<SocialProofController> logger,
        IActivityLogService activityLogService,
        IUserContextService userContextService,
        ApplicationDbContext context) : base(userContextService, context)
    {
        _socialProofService = socialProofService;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index()
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return RedirectToAction("Login", "Account");
        }

        var settings = await _socialProofService.GetOrCreateSettingsAsync(companyId, userId);
        
        var viewModel = new SocialProofIndexViewModel
        {
            Settings = settings,
            ScriptUrl = Url.Action("Script", "SocialProofApi", new { companyId }, Request.Scheme)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSocialProofSettingsViewModel model)
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        var settings = new SocialProofSettings
        {
            CompanyId = companyId,
            IsActive = model.IsActive,
            IsViewersActive = model.IsViewersActive,
            ViewersMin = model.ViewersMin,
            ViewersMax = model.ViewersMax,
            IsFollowersActive = model.IsFollowersActive,
            FollowersMin = model.FollowersMin,
            FollowersMax = model.FollowersMax,
            IsBuyersActive = model.IsBuyersActive,
            BuyersMin = model.BuyersMin,
            BuyersMax = model.BuyersMax,
            UpdateInterval = model.UpdateInterval,
            DisplayDuration = model.DisplayDuration,
            TextTemplates = new SocialProofTextTemplates
            {
                ViewersTemplate = model.ViewersTemplate,
                FollowersTemplate = model.FollowersTemplate,
                BuyersTemplate = model.BuyersTemplate
            },
            DisplaySettings = new SocialProofDisplaySettings
            {
                Position = model.Position,
                PrimaryColor = model.PrimaryColor,
                TextColor = model.TextColor,
                BackgroundColor = model.BackgroundColor,
                BorderRadius = model.BorderRadius,
                FontSize = model.FontSize,
                EnableAnimation = model.EnableAnimation,
                EnableShadow = model.EnableShadow
            }
        };

        var success = await _socialProofService.UpdateSettingsAsync(companyId, settings, userId);

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Update,
                ActivityModules.SocialProof,
                $"Sosyal kanıt ayarları güncellendi (Aktif: {model.IsActive})",
                entityType: "SocialProofSettings",
                details: new { model.IsActive, model.Position, model.ViewersMin, model.ViewersMax },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SettingsSaved);
        }
        else
        {
            return Json(ApiResponse.Error("Ayarlar güncellenirken bir hata oluştu."));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus()
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        var success = await _socialProofService.ToggleActiveStatusAsync(companyId, userId);

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.StatusChange,
                ActivityModules.SocialProof,
                "Sosyal kanıt modülü durumu değiştirildi",
                entityType: "SocialProofSettings",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SuccessMessage("Durum başarıyla güncellendi."));
        }
        else
        {
            return Json(ApiResponse.Error("Durum güncellenirken bir hata oluştu."));
        }
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> Preview()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var config = await _socialProofService.GetScriptConfigAsync(companyId);

        return Json(new {
            success = true,
            config = config,
            scriptUrl = Url.Action("Script", "SocialProofApi", new { companyId = companyId }, Request.Scheme)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireCompanyId]
    public async Task<IActionResult> ExportToR2()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var jsonUrl = await _socialProofService.ExportSocialProofToR2Async(companyId);

        _logger.LogInformation("Social proof exported to R2 for company {CompanyId}", companyId);

        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Export,
                ActivityModules.SocialProof,
                "Sosyal kanıt siteye aktarıldı",
                entityType: "SocialProofSettings",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success = true, message = "Siteye başarıyla aktarıldı!", jsonUrl = jsonUrl });
    }
}
