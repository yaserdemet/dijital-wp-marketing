using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.Services.Modules.VideoHosting;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class VideoHostingController : BaseController
{
    private readonly IVideoHostingService _videoHostingService;
    private readonly IVideoHostingModuleService _videoHostingModuleService;
    private readonly IActivityLogService _activityLogService;

    public VideoHostingController(
        IVideoHostingService videoHostingService,
        IVideoHostingModuleService videoHostingModuleService,
        IActivityLogService activityLogService,
        ApplicationDbContext context,
        IUserContextService userContextService)
        : base(userContextService, context)
    {
        _videoHostingService = videoHostingService;
        _videoHostingModuleService = videoHostingModuleService;
        _activityLogService = activityLogService;
    }

    #region Main Views

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction("Index", "Home");
        }

        // Check if module is active
        var hasActiveModule = await _videoHostingModuleService.HasActiveModuleAsync(companyId.Value);
        if (!hasActiveModule)
        {
            return RedirectToAction("Index", "Store");
        }

        var videos = await _videoHostingService.GetVideosAsync(companyId.Value);
        var stats = await _videoHostingService.GetVideoStatsAsync(companyId.Value);

        ViewBag.Stats = stats;
        return View(videos);
    }

    public async Task<IActionResult> Upload()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction("Index", "Home");
        }

        // Check if module is active
        var hasActiveModule = await _videoHostingModuleService.HasActiveModuleAsync(companyId.Value);
        if (!hasActiveModule)
        {
            return RedirectToAction("Index", "Store");
        }

        var settings = await _videoHostingService.GetSettingsAsync(companyId.Value);
        ViewBag.Settings = settings;

        return View();
    }

    public async Task<IActionResult> Settings()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction("Index", "Home");
        }

        // Check if module is active
        var hasActiveModule = await _videoHostingModuleService.HasActiveModuleAsync(companyId.Value);
        if (!hasActiveModule)
        {
            return RedirectToAction("Index", "Store");
        }

        var settings = await _videoHostingService.GetSettingsAsync(companyId.Value);
        return View(settings);
    }

    #endregion

    #region AJAX Actions

    [HttpPost]
    [RequireCompanyId]
    [ValidateModelState]
    public async Task<IActionResult> UploadVideo(VideoUploadRequestDto request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Json(ApiResponse.Error("Kullanıcı bilgileri bulunamadı."));
        }

        var result = await _videoHostingService.UploadVideoAsync(companyId, request, userId);

        if (result.Success && result.VideoId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Create,
                ActivityModules.VideoHosting,
                $"Video yüklendi: {request.Title}",
                entityId: result.VideoId.Value.ToString(),
                entityType: "Video",
                details: new { request.Title },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            videoId = result.VideoId
        });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetVideo(Guid id)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var video = await _videoHostingService.GetVideoByIdAsync(companyId, id);

        if (video == null)
        {
            return Json(ApiResponse.Error("Video bulunamadı."));
        }

        return Json(new { success = true, data = video });
    }

    [HttpPost]
    [RequireCompanyId]
    [ValidateModelState]
    public async Task<IActionResult> UpdateVideo(Guid id, VideoUpdateRequestDto request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Json(ApiResponse.Error("Kullanıcı bilgileri bulunamadı."));
        }

        var result = await _videoHostingService.UpdateVideoAsync(companyId, id, request, userId);

        if (result.Success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Update,
                ActivityModules.VideoHosting,
                $"Video güncellendi (ID: {id})",
                entityId: id.ToString(),
                entityType: "Video",
                details: new { request.Title },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> DeleteVideo(Guid id)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Json(ApiResponse.Error("Kullanıcı bilgileri bulunamadı."));
        }

        var result = await _videoHostingService.DeleteVideoAsync(companyId, id, userId);

        if (result.Success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Delete,
                ActivityModules.VideoHosting,
                $"Video silindi (ID: {id})",
                entityId: id.ToString(),
                entityType: "Video",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [RequireCompanyId]
    [ValidateModelState]
    public async Task<IActionResult> UpdateSettings(VideoSettingsRequestDto request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Json(ApiResponse.Error("Kullanıcı bilgileri bulunamadı."));
        }

        var result = await _videoHostingService.UpdateSettingsAsync(companyId, request, userId);

        if (result.Success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Update,
                ActivityModules.VideoHosting,
                "Video hosting ayarları güncellendi",
                entityType: "VideoSettings",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GenerateEmbedCode(Guid id, int width = 640, int height = 360, bool autoPlay = false, bool muted = false, bool showControls = true)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var embedData = await _videoHostingService.GenerateEmbedCodeAsync(companyId, id, width, height, autoPlay, muted, showControls);

        if (embedData == null)
        {
            return Json(ApiResponse.Error("Embed kodu oluşturulamadı."));
        }

        return Json(new { success = true, data = embedData });
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> ValidateUpload(IFormFile videoFile)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var result = await _videoHostingService.ValidateVideoUploadAsync(companyId, videoFile);

        return Json(new
        {
            success = result.CanUpload,
            message = result.Message,
            requiredCredits = result.RequiredCredits
        });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetQuotaInfo()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var quota = await _videoHostingService.GetQuotaInfoAsync(companyId);

        if (!quota.Found)
            return Json(new { success = false, message = "Aktif bir Video Hosting paketiniz bulunmuyor." });

        return Json(new
        {
            success = true,
            data = new
            {
                packageName = quota.PackageName,
                totalMB = quota.TotalMB,
                remainingMB = quota.RemainingMB,
                usedMB = quota.TotalMB - quota.RemainingMB,
                totalGB = Math.Round(quota.TotalMB / 1024m, 2),
                remainingGB = Math.Round(quota.RemainingMB / 1024m, 2)
            }
        });
    }

    #endregion

    #region Public Player Actions

    [AllowAnonymous]
    public async Task<IActionResult> Player(Guid id, string? token = null, bool? autoplay = null, bool? muted = null, bool? controls = null, int? w = null, int? h = null)
    {
        var playerData = await _videoHostingService.GetPublicVideoPlayerDataAsync(id, token, autoplay, muted, controls);

        if (playerData == null)
        {
            return NotFound("Video bulunamadı veya erişim izniniz yok.");
        }

        // Use requested dimensions if provided
        if (w.HasValue && w.Value > 0) playerData.Width = w.Value;
        if (h.HasValue && h.Value > 0) playerData.Height = h.Value;

        // Record view
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var referrer = HttpContext.Request.Headers["Referer"].ToString();

        await _videoHostingService.RecordVideoViewAsync(id, ipAddress, userAgent, referrer);

        return View(playerData);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Embed(Guid id, string? domain = null, bool? autoplay = null, bool? muted = null, bool? controls = null, int? w = null, int? h = null)
    {
        var playerData = await _videoHostingService.GetEmbedPlayerDataAsync(id, domain, autoplay, muted, controls);

        if (playerData == null)
        {
            return NotFound("Video bulunamadı veya embed edilemez.");
        }

        // Use requested dimensions if provided
        if (w.HasValue && w.Value > 0) playerData.Width = w.Value;
        if (h.HasValue && h.Value > 0) playerData.Height = h.Value;

        // Record view
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var referrer = HttpContext.Request.Headers["Referer"].ToString();

        await _videoHostingService.RecordVideoViewAsync(id, ipAddress, userAgent, referrer, domain);

        return View(playerData);
    }

    /// <summary>
    /// Direct video file redirect - returns 302 to the actual video file URL.
    /// Use this when you need a direct link to the video file for product cards, Open Graph, etc.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> DirectVideo(Guid id, string? token = null)
    {
        var videoUrl = await _videoHostingService.GetDirectVideoUrlAsync(id, token);

        if (videoUrl == null)
        {
            return NotFound("Video bulunamadı veya erişim izniniz yok.");
        }

        // Record view
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var referrer = HttpContext.Request.Headers["Referer"].ToString();

        await _videoHostingService.RecordVideoViewAsync(id, ipAddress, userAgent, referrer);

        return Redirect(videoUrl);
    }

    #endregion
}
