using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using PushDashboard.Services.Integrations;
using PushDashboard.Services;

namespace PushDashboard.Controllers;

[Authorize]
public class OrderStatusController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderStatusController> _logger;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IWhatsAppTemplateService _whatsAppTemplateService;
    private readonly ISmsTemplateService _smsTemplateService;
    private readonly IActivityLogService _activityLogService;

    public OrderStatusController(
        ApplicationDbContext context, 
        ILogger<OrderStatusController> logger, 
        IEmailTemplateService emailTemplateService, 
        IWhatsAppTemplateService whatsAppTemplateService, 
        ISmsTemplateService smsTemplateService,
        IActivityLogService activityLogService, 
        IUserContextService userContextService)
        : base(userContextService, context)
    {
        _context = context;
        _logger = logger;
        _emailTemplateService = emailTemplateService;
        _whatsAppTemplateService = whatsAppTemplateService;
        _smsTemplateService = smsTemplateService;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return RedirectToAction("Index", "Home");
        }

        // Aktif iletişim kanallarını kontrol et
        var activeChannels = await GetActiveChannels(companyId.Value);

        var viewModel = new OrderStatusNotificationIndexViewModel
        {
            OrderStatuses = GetOrderStatuses(),
            Notifications = await GetOrderStatusNotifications(companyId.Value),
            EmailTemplates = activeChannels.HasEmail ? await GetEmailTemplates(companyId.Value) : new List<TemplateSelectItem>(),
            SmsTemplates = activeChannels.HasSms ? await GetSmsTemplates(companyId.Value) : new List<TemplateSelectItem>(),
            WhatsAppTemplates = activeChannels.HasWhatsApp ? await GetWhatsAppTemplates(companyId.Value) : new List<TemplateSelectItem>(),
            ActiveChannels = activeChannels
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> SaveNotificationSettings([FromBody] SaveOrderStatusNotificationRequest request)
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        // Mevcut ayarı bul veya yeni oluştur
        var notification = await _context.OrderStatusNotifications
            .FirstOrDefaultAsync(osn => osn.CompanyId == companyId.Value && osn.OrderStatus == request.OrderStatus);

        if (notification == null)
        {
            notification = new OrderStatusNotification
            {
                CompanyId = companyId.Value,
                OrderStatus = request.OrderStatus,
                OrderStatusDisplayName = GetOrderStatusDisplayName(request.OrderStatus),
                CreatedAt = DateTime.Now
            };
            _context.OrderStatusNotifications.Add(notification);
        }

        // Ayarları güncelle
        notification.IsActive = request.IsActive;
        notification.EmailNotificationEnabled = request.EmailNotificationEnabled;
        notification.EmailTemplateId = request.EmailTemplateId;
        notification.SmsNotificationEnabled = request.SmsNotificationEnabled;
        notification.SmsTemplateId = request.SmsTemplateId;
        notification.WhatsAppNotificationEnabled = request.WhatsAppNotificationEnabled;
        notification.WhatsAppTemplateId = request.WhatsAppTemplateId;
        notification.DelayMinutes = request.DelayMinutes;
        notification.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order status notification settings saved for company {CompanyId}, status {OrderStatus}",
            companyId.Value, request.OrderStatus);

        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId.Value,
                ActivityActionTypes.Update,
                ActivityModules.OrderStatus,
                $"Sipariş durumu bildirimi ayarları güncellendi: {GetOrderStatusDisplayName(request.OrderStatus)}",
                entityId: notification.Id.ToString(),
                entityType: "OrderStatusNotification",
                details: new { request.OrderStatus, request.IsActive, request.EmailNotificationEnabled, request.WhatsAppNotificationEnabled },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(ApiResponse.SuccessMessage("Ayarlar başarıyla kaydedildi."));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleNotification([FromBody] ToggleNotificationRequest request)
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var notification = await _context.OrderStatusNotifications
            .FirstOrDefaultAsync(osn => osn.CompanyId == companyId.Value && osn.OrderStatus == request.OrderStatus);

        if (notification == null)
        {
            return Json(ApiResponse.Error("Bildirim ayarı bulunamadı."));
        }

        notification.IsActive = request.IsActive;
        notification.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId.Value,
                ActivityActionTypes.StatusChange,
                ActivityModules.OrderStatus,
                $"Sipariş durumu bildirimi {(request.IsActive ? "aktif" : "pasif")} yapıldı: {GetOrderStatusDisplayName(request.OrderStatus)}",
                entityId: notification.Id.ToString(),
                entityType: "OrderStatusNotification",
                details: new { request.OrderStatus, request.IsActive },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(ApiResponse.SuccessMessage("Bildirim durumu güncellendi."));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderStatusLogs(int page = 1, int pageSize = 20, string? orderStatus = null)
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var query = _context.OrderStatusChangeLogs
            .Where(oscl => oscl.CompanyId == companyId.Value);

        if (!string.IsNullOrEmpty(orderStatus))
        {
            query = query.Where(oscl => oscl.NewStatus == orderStatus);
        }

        var totalCount = await query.CountAsync();
        var logs = await query
            .OrderByDescending(oscl => oscl.StatusChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(oscl => new OrderStatusChangeLogViewModel
            {
                Id = oscl.Id,
                OrderId = oscl.OrderId,
                OrderNumber = oscl.OrderNumber,
                CustomerEmail = oscl.CustomerEmail,
                CustomerName = oscl.CustomerName,
                OldStatus = oscl.OldStatus,
                NewStatus = oscl.NewStatus,
                NewStatusDisplayName = oscl.NewStatusDisplayName,
                StatusChangedAt = oscl.StatusChangedAt,
                NotificationSent = oscl.NotificationSent,
                NotificationChannels = oscl.NotificationChannels
            })
            .ToListAsync();

        return Json(new
        {
            success = true,
            data = logs,
            totalCount = totalCount,
            currentPage = page,
            pageSize = pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }





    private async Task<ActiveChannelsViewModel> GetActiveChannels(Guid companyId)
    {
        var hasEmail = await _emailTemplateService.HasSmtpConfigurationAsync(companyId);
        var hasWhatsApp = await _whatsAppTemplateService.HasWhatsAppConfigurationAsync(companyId);
        var hasSms = await _smsTemplateService.HasSmsIntegrationAsync(companyId);

        return new ActiveChannelsViewModel
        {
            HasEmail = hasEmail,
            HasSms = hasSms,
            HasWhatsApp = hasWhatsApp
        };
    }

    private async Task<List<OrderStatusNotificationViewModel>> GetOrderStatusNotifications(Guid companyId)
    {
        var notifications = await _context.OrderStatusNotifications
            .Where(osn => osn.CompanyId == companyId)
            .ToListAsync();

        var result = new List<OrderStatusNotificationViewModel>();

        // Tüm sipariş durumları için ayarları oluştur
        foreach (var status in GetOrderStatuses())
        {
            var notification = notifications.FirstOrDefault(n => n.OrderStatus == status.Value);
            result.Add(new OrderStatusNotificationViewModel
            {
                OrderStatus = status.Value,
                OrderStatusDisplayName = status.Text,
                IsActive = notification?.IsActive ?? false,
                EmailNotificationEnabled = notification?.EmailNotificationEnabled ?? false,
                EmailTemplateId = notification?.EmailTemplateId,
                SmsNotificationEnabled = notification?.SmsNotificationEnabled ?? false,
                SmsTemplateId = notification?.SmsTemplateId,
                WhatsAppNotificationEnabled = notification?.WhatsAppNotificationEnabled ?? false,
                WhatsAppTemplateId = notification?.WhatsAppTemplateId,
                DelayMinutes = notification?.DelayMinutes ?? 0,
                TotalNotificationsSent = notification?.TotalNotificationsSent ?? 0,
                LastNotificationAt = notification?.LastNotificationAt
            });
        }

        return result;
    }

    private async Task<List<TemplateSelectItem>> GetEmailTemplates(Guid companyId)
    {
        return (await _emailTemplateService.GetAvailableTemplatesAsync(companyId))
            .Select(cet => new TemplateSelectItem
            {
                Id = cet.Id,
                Name = cet.Name,
                Category = cet.Category
            })
            .ToList();
    }

    private async Task<List<TemplateSelectItem>> GetSmsTemplates(Guid companyId)
    {
        var templates = await _smsTemplateService.GetUserTemplatesAsync(companyId);
        return templates.Select(t => new TemplateSelectItem
        {
            Id = t.Id,
            Name = t.Name,
            Category = t.Category
        }).ToList();
    }

    private async Task<List<TemplateSelectItem>> GetWhatsAppTemplates(Guid companyId)
    {
        return (await _whatsAppTemplateService.GetFacebookTemplatesAsync(companyId))
            .Where(x=> x.Status == "APPROVED")
            .Select(t => new TemplateSelectItem
            {
                WhatsappId = t.Id,
                Name = t.Name,
                Category = t.Category
            })
            .ToList();
    }

    private List<OrderStatusSelectItem> GetOrderStatuses()
    {
        return new List<OrderStatusSelectItem>
        {
            new() { Value = "1", Text = "Beklemede" },
            new() { Value = "2", Text = "Onaylandı" },
            new() { Value = "3", Text = "Hazırlanıyor" },
            new() { Value = "4", Text = "Kargoya Verildi" },
            new() { Value = "5", Text = "Teslim Edildi" },
            new() { Value = "6", Text = "İptal Edildi" },
            new() { Value = "7", Text = "İade Edildi" }
        };
    }

    private string GetOrderStatusDisplayName(string status)
    {
        var statusItem = GetOrderStatuses().FirstOrDefault(s => s.Value == status);
        return statusItem?.Text ?? status;
    }
}
