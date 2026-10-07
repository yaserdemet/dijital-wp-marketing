using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.Services.Modules.BasketReminder;
using System.Security.Claims;
using PushDashboard.Data;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class BasketReminderController : BaseController
{
    private readonly IBasketReminderService _basketReminderService;
    private readonly IBasketReminderModuleService _moduleService;
    private readonly IActivityLogService _activityLogService;

    public BasketReminderController(
        IBasketReminderService basketReminderService,
        IBasketReminderModuleService moduleService,
        IActivityLogService activityLogService,
        ApplicationDbContext context,
        IUserContextService userContextService) : base(userContextService, context)
    {
        _basketReminderService = basketReminderService;
        _moduleService = moduleService;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Ana sayfa - Basit hatırlatma listesi
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction("Index", "Home");
        }

        // Check if company has active module
        if (!await _moduleService.HasActiveModuleAsync(companyId.Value))
        {
            ViewBag.ErrorMessage = "Sepet hatırlatma modülü aktif değil. Lütfen modülü satın alın.";
            return View("ModuleNotActive");
        }

        var reminders = await _basketReminderService.GetAllSchedulesAsync(companyId.Value) ?? new List<BasketReminderSchedule>();
        return View(reminders);
    }

    /// <summary>
    /// Yeni hatırlatma oluştur
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> Create([FromBody] CreateReminderRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        if (!await _moduleService.HasActiveModuleAsync(companyId))
        {
            return Json(ApiResponse.Error("Sepet hatırlatma modülü aktif değil. Lütfen modülü satın alın."));
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Kullanıcı bilgisi bulunamadı."));
        }

        await _basketReminderService.CreateScheduleAsync(
            companyId,
            request.Name,
            request.Message,
            request.Hours,
            request.IsActive,
            request.AllowResendForSentCustomers,
            request.CommunicationChannels,
            request.ChannelMessages,
            userId);

        // Activity Log
        await _activityLogService.LogActivityAsync(
            userId,
            companyId,
            ActivityActionTypes.Create,
            ActivityModules.BasketReminder,
            $"Yeni sepet hatırlatma zamanlaması oluşturuldu: {request.Name}",
            entityType: "BasketReminderSchedule",
            details: new { request.Name, request.Hours, request.IsActive, Channels = request.CommunicationChannels },
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(new { success = true, message = "Hatırlatma zamanlaması başarıyla oluşturuldu." });
    }

    /// <summary>
    /// Hatırlatma güncelle
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> Update([FromBody] UpdateReminderRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Kullanıcı bilgisi bulunamadı."));
        }

        await _basketReminderService.UpdateScheduleAsync(
            companyId,
            request.Id,
            request.Name,
            request.Message,
            request.Hours,
            request.IsActive,
            request.AllowResendForSentCustomers,
            request.CommunicationChannels,
            request.ChannelMessages,
            userId);

        // Activity Log
        await _activityLogService.LogActivityAsync(
            userId,
            companyId,
            ActivityActionTypes.Update,
            ActivityModules.BasketReminder,
            $"Sepet hatırlatma zamanlaması güncellendi: {request.Name}",
            entityId: request.Id.ToString(),
            entityType: "BasketReminderSchedule",
            details: new { request.Id, request.Name, request.Hours, request.IsActive, Channels = request.CommunicationChannels },
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(new { success = true, message = "Hatırlatma zamanlaması başarıyla güncellendi." });
    }

    /// <summary>
    /// Hatırlatma sil
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> Delete([FromBody] DeleteReminderRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Kullanıcı bilgisi bulunamadı."));
        }

        await _basketReminderService.DeleteScheduleAsync(companyId, request.Id, userId);

        // Activity Log
        await _activityLogService.LogActivityAsync(
            userId,
            companyId,
            ActivityActionTypes.Delete,
            ActivityModules.BasketReminder,
            $"Sepet hatırlatma zamanlaması silindi (ID: {request.Id})",
            entityId: request.Id.ToString(),
            entityType: "BasketReminderSchedule",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(new { success = true, message = "Hatırlatma zamanlaması başarıyla silindi." });
    }

    /// <summary>
    /// Hatırlatma durumunu değiştir
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Kullanıcı bilgisi bulunamadı."));
        }

        await _basketReminderService.ToggleScheduleStatusAsync(companyId, request.Id, userId);

        // Activity Log
        await _activityLogService.LogActivityAsync(
            userId,
            companyId,
            ActivityActionTypes.StatusChange,
            ActivityModules.BasketReminder,
            $"Sepet hatırlatma zamanlaması durumu değiştirildi (ID: {request.Id})",
            entityId: request.Id.ToString(),
            entityType: "BasketReminderSchedule",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(new { success = true, message = "Zamanlama durumu başarıyla güncellendi." });
    }

    // Request Models
    public class CreateReminderRequest
    {
        public string Name { get; set; } = "";
        public string Message { get; set; } = "";
        public int Hours { get; set; }
        public bool IsActive { get; set; } = true;
        public bool AllowResendForSentCustomers { get; set; } = false;
        public string CommunicationChannels { get; set; } = "[]";
        public string ChannelMessages { get; set; } = "{}";
    }

    public class UpdateReminderRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Message { get; set; } = "";
        public int Hours { get; set; }
        public bool IsActive { get; set; }
        public bool AllowResendForSentCustomers { get; set; } = false;
        public string CommunicationChannels { get; set; } = "[]";
        public string ChannelMessages { get; set; } = "{}";
    }

    public class DeleteReminderRequest
    {
        public int Id { get; set; }
    }

    public class ToggleStatusRequest
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Belirli bir schedule için logları getir (filtreleme ve sayfalama ile)
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetScheduleLogs(
        int? scheduleId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? isSuccessful = null,
        int page = 1,
        int pageSize = 20)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        var (logs, hasMore) = await _basketReminderService.GetLogsForScheduleAsync(
            companyId,
            scheduleId,
            startDate,
            endDate,
            isSuccessful,
            page,
            pageSize);

        // DTO'ya dönüştür
        var logDtos = logs.Select(log => new
        {
            id = log.Id,
            basketExternalId = log.BasketExternalId,
            customerName = log.CustomerName,
            customerEmail = log.CustomerEmail,
            reminderTimeForHours = log.ReminderTimeForHours,
            reminderTimeText = log.ReminderTimeText,
            scheduleName = log.BasketReminderSchedule?.Name,
            sentAt = log.SentAt,
            isSuccessful = log.IsSuccessful,
            errorMessage = log.ErrorMessage,
            notificationContent = log.NotificationContent,
            statusText = log.StatusText,
            statusBadgeClass = log.StatusBadgeClass
        }).ToList();

        return Json(new
        {
            success = true,
            logs = logDtos,
            hasMore = hasMore,
            page = page,
            pageSize = pageSize
        });
    }

    /// <summary>
    /// Tüm hatırlatma zamanlamalarını JSON olarak getir (Yönetim paneli için)
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetAllSchedules()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        
        // This service method should return all schedules, active or not
        var schedules = await _basketReminderService.GetAllSchedulesAsync(companyId);

        return Json(new { 
            success = true, 
            data = schedules.Select(s => new {
                id = s.Id,
                name = s.Name,
                hours = s.ReminderTimeForHours,
                reminderTimeText = s.ReminderTimeText,
                isActive = s.IsActive,
                allowResendForSentCustomers = s.AllowResendForSentCustomers,
                communicationChannels = s.CommunicationChannels, // JSON string
                channelMessages = s.ChannelMessages, // JSON string
                notificationContent = s.NotificationContent // Legacy?
            })
        });
    }
}
