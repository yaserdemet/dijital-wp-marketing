using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.Services.BulkMessaging;
using PushDashboard.ViewModels;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class BulkMessagingController : BaseController
{
    private readonly IBulkMessagingService _bulkMessagingService;
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;

    public BulkMessagingController(
        IBulkMessagingService bulkMessagingService,
        ApplicationDbContext context,
        IActivityLogService activityLogService,
        IUserContextService userContextService)
        : base(userContextService,context)
    {
        _bulkMessagingService = bulkMessagingService;
        _context = context;
        _activityLogService = activityLogService;
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> CheckModuleAccess()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        var hasModule = await _bulkMessagingService.HasBulkMessagingModuleAsync(companyId);
        if (!hasModule)
        {
            return Json(ApiResponse.Error("Toplu mesaj gönderimi modülüne sahip değilsiniz."));
        }

        var enabledChannels = await _bulkMessagingService.GetEnabledChannelsAsync(companyId);
        if (!enabledChannels.Any())
        {
            return Json(new {
                success = false,
                message = "Henüz hiçbir iletişim kanalı aktif edilmemiş. Lütfen modül ayarlarından kanalları aktif edin.",
                needsConfiguration = true
            });
        }

        return Json(new { success = true, enabledChannels });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetAvailableChannels()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var channels = await _bulkMessagingService.GetAvailableChannelsAsync(companyId);
        return Json(new { success = true, data = channels });
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> EstimateRecipients([FromBody] CustomerFilterViewModel filters)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var count = await _bulkMessagingService.EstimateRecipientsAsync(companyId, filters);
        return Json(new { success = true, count });
    }

    [HttpPost]
    [RequireCompanyId]
    [ValidateModelState]
    public async Task<IActionResult> CreatePreview([FromBody] CreateBulkMessageViewModel model)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var preview = await _bulkMessagingService.CreatePreviewAsync(companyId, model);
        return Json(new { success = true, data = preview });
    }

    [HttpPost]
    [RequireCompanyId]
    [ValidateModelState]
    public async Task<IActionResult> CreateBulkMessage([FromBody] CreateBulkMessageViewModel model)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Kullanıcı bilgisi bulunamadı."));
        }

        var (success, message, bulkMessageId) = await _bulkMessagingService.CreateBulkMessageAsync(companyId, userId, model);

        if (success && bulkMessageId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Create,
                ActivityModules.BulkMessaging,
                $"Toplu mesaj oluşturuldu: {model.Title}",
                entityId: bulkMessageId.Value.ToString(),
                entityType: "BulkMessage",
                details: new { model.Title, model.Description },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new { success = true, message, bulkMessageId = bulkMessageId.Value });
        }
        else
        {
            return Json(new { success = false, message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> StartBulkMessage(int bulkMessageId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Kullanıcı bilgisi bulunamadı."));
        }

        var (success, message) = await _bulkMessagingService.StartBulkMessageAsync(bulkMessageId, userId);

        if (success)
        {
            var companyId = GetCurrentUserCompanyId();
            if (companyId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.Send,
                    ActivityModules.BulkMessaging,
                    $"Toplu mesaj gönderimi başlatıldı (ID: {bulkMessageId})",
                    entityId: bulkMessageId.ToString(),
                    entityType: "BulkMessage",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
        }

        return Json(new { success, message });
    }

    [HttpPost]
    public async Task<IActionResult> CancelBulkMessage(int bulkMessageId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Kullanıcı bilgisi bulunamadı."));
        }

        var (success, message) = await _bulkMessagingService.CancelBulkMessageAsync(bulkMessageId, userId);

        if (success)
        {
            var companyId = GetCurrentUserCompanyId();
            if (companyId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.StatusChange,
                    ActivityModules.BulkMessaging,
                    $"Toplu mesaj iptal edildi (ID: {bulkMessageId})",
                    entityId: bulkMessageId.ToString(),
                    entityType: "BulkMessage",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
        }

        return Json(new { success, message });
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> ResendBulkMessage(int originalBulkMessageId, int startIndex, int count)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Kullanıcı bilgisi bulunamadı."));
        }

        var (success, message, bulkMessageId) = await _bulkMessagingService.ResendBulkMessageAsync(
            originalBulkMessageId, startIndex, count, companyId, userId);

        if (success && bulkMessageId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Send,
                ActivityModules.BulkMessaging,
                $"Toplu mesaj tekrar gönderimi başlatıldı (Orijinal ID: {originalBulkMessageId}, Yeni ID: {bulkMessageId})",
                entityId: bulkMessageId.Value.ToString(),
                entityType: "BulkMessage",
                details: new { originalBulkMessageId, startIndex, count },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new { success = true, message, bulkMessageId = bulkMessageId.Value });
        }
        else
        {
            return Json(new { success = false, message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetProgress(int bulkMessageId)
    {
        var progress = await _bulkMessagingService.GetProgressAsync(bulkMessageId);
        if (progress == null)
        {
            return Json(ApiResponse.Error("Toplu mesaj bulunamadı."));
        }

        return Json(new { success = true, data = progress });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetBulkMessageDetails(int bulkMessageId)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var details = await _bulkMessagingService.GetBulkMessageDetailsAsync(bulkMessageId, companyId);
        if (details == null)
        {
            return Json(ApiResponse.Error("Toplu mesaj bulunamadı."));
        }

        return Json(new { success = true, data = details });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetHistory(int page = 1, int pageSize = 20)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var history = await _bulkMessagingService.GetBulkMessageHistoryAsync(companyId, page, pageSize);
        return Json(new { success = true, data = history });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetStats()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var stats = await _bulkMessagingService.GetStatsAsync(companyId);
        return Json(new { success = true, data = stats });
    }

    [HttpGet]
    public async Task<IActionResult> GetJobHistory()
    {
        var companyId = GetCurrentCompanyId();

        var jobs = await _context.BulkMessages
            .Where(bm => bm.CompanyId == companyId)
            .OrderByDescending(bm => bm.CreatedAt)
            .Take(20) // Son 20 job
            .Select(bm => new
            {
                bm.Id,
                bm.Title,
                bm.Description,
                bm.Status,
                bm.TotalRecipients,
                bm.ProcessedRecipients,
                bm.SuccessfulSends,
                bm.FailedSends,
                bm.CurrentBatch,
                bm.TotalBatches,
                bm.EstimatedCost,
                bm.ActualCost,
                bm.CreatedAt,
                bm.CompletedAt,
                bm.ErrorMessage,
                bm.ChannelSettingsJson,
                bm.CustomerFiltersJson
            })
            .ToListAsync();

        return Json(new { success = true, data = jobs });
    }

    [HttpGet]
    public async Task<IActionResult> GetJobDetails(int id)
    {
        var companyId = GetCurrentCompanyId();

        var job = await _context.BulkMessages
            .Where(bm => bm.Id == id && bm.CompanyId == companyId)
            .Select(bm => new
            {
                bm.Id,
                bm.Title,
                bm.Description,
                bm.Status,
                bm.TotalRecipients,
                bm.ProcessedRecipients,
                bm.SuccessfulSends,
                bm.FailedSends,
                bm.CurrentBatch,
                bm.TotalBatches,
                bm.EstimatedCost,
                bm.ActualCost,
                bm.CreatedAt,
                bm.CompletedAt,
                bm.ErrorMessage,
                bm.ChannelSettingsJson,
                bm.CustomerFiltersJson
            })
            .FirstOrDefaultAsync();

        if (job == null)
        {
            return Json(ApiResponse.Error("Job bulunamadı."));
        }

        return Json(new { success = true, data = job });
    }

    private Guid GetCurrentCompanyId()
    {
        var companyId = GetCurrentUserCompanyId();
        return companyId ?? Guid.Empty; // Fallback to empty guid if not found
    }
}
