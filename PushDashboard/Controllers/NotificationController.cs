using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.Integrations.Common.Models;
using PushDashboard.Services.Modules.Birthday;

namespace PushDashboard.Controllers;

public class NotificationController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IBirthdayModuleService _birthdayModuleService;
    private readonly ILogger<NotificationController> _logger;
    private readonly IActivityLogService _activityLogService;

    private readonly IEcommerceServiceFactory _ecommerceServiceFactory;

    public NotificationController(
        ApplicationDbContext context,
        IBirthdayModuleService birthdayModuleService,
        ILogger<NotificationController> logger,
        IActivityLogService activityLogService,
        IUserContextService userContextService,
        IEcommerceServiceFactory ecommerceServiceFactory)
        : base(userContextService, context)
    {
        _context = context;
        _birthdayModuleService = birthdayModuleService;
        _logger = logger;
        _activityLogService = activityLogService;
        _ecommerceServiceFactory = ecommerceServiceFactory;
    }

    /// <summary>
    /// Belirtilen şirket için doğum günü bildirimleri gönderir
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SendBirthdayNotifications()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Kullanıcı bilgisi bulunamadı."));
        }

        var result = await _birthdayModuleService.SendBirthdayNotificationsAsync(companyId.Value, userId);

        if (result.Success)
        {
            if (result.NotificationsSent > 0)
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.Send,
                    ActivityModules.Notification,
                    $"Doğum günü bildirimleri gönderildi. ({result.NotificationsSent} kişi) - Toplam Maliyet: {result.TotalCost:N2} TL",
                    entityType: "BirthdayNotification",
                    details: new { result.NotificationsSent, result.TotalCost, IsAutomatic = false },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
            else
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.Send,
                    ActivityModules.Notification,
                    "Doğum günü bildirimleri gönderilmedi.Bugün doğum günü olan müşteri bulunamadı.",
                    entityType: "BirthdayNotification",
                    details: new { NotificationsSent = 0, TotalCost = 0m, IsAutomatic = false },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
        }
        else
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId.Value,
                ActivityActionTypes.Send,
                ActivityModules.Notification,
                $"Doğum günü bildirimleri gönderilemedi: {result.Message}",
                entityType: "BirthdayNotification",
                details: new { Error = result.Message, IsAutomatic = false },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            notificationsSent = result.NotificationsSent,
            totalCost = result.TotalCost
        });
    }

    /// <summary>
    /// Bugün doğum günü olan müşterileri listeler (önizleme için)
    /// Ticimax'tan güncel doğum günü bilgilerini çekip veritabanını günceller
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetTodaysBirthdays()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var today = DateTime.Now.Date;

        try
        {
            var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId.Value);
            if (ecommerceService != null)
            {
                _logger.LogInformation("Syncing today's birthday customers from Ticimax for company {CompanyId}", companyId.Value);

                var filter = new EcommerceCustomerFilter
                {
                    IsActive = true
                };
                var pagination = new EcommercePagination
                {
                    PageNumber = 1,
                    PageSize = 1000
                };

                var ticimaxCustomers = await ecommerceService.GetCustomersAsync(companyId.Value, filter, pagination);

                if (ticimaxCustomers != null && ticimaxCustomers.Length > 0)
                {
                    var todayBirthdayTicimaxCustomers = ticimaxCustomers
                        .Where(c => c.BirthDate.HasValue)
                        .Select(c => new { Customer = c, LocalBirthDate = c.BirthDate!.Value })
                        .Where(x => x.LocalBirthDate.Month == today.Month && x.LocalBirthDate.Day == today.Day)
                        .Select(x => x.Customer)
                        .ToList();

                    _logger.LogInformation("Found {Count} customers with today's birthday in Ticimax for company {CompanyId}",
                        todayBirthdayTicimaxCustomers.Count, companyId.Value);

                    foreach (var ticimaxCustomer in todayBirthdayTicimaxCustomers)
                    {
                        if (!int.TryParse(ticimaxCustomer.ExternalId, out var externalId))
                            continue;

                        var existingCustomer = await _context.Customers
                            .FirstOrDefaultAsync(c => c.ExternalId == externalId && c.CompanyId == companyId.Value);

                        if (existingCustomer != null)
                        {
                            existingCustomer.BirthDate = ticimaxCustomer.BirthDate!.Value;
                            existingCustomer.FirstName = string.IsNullOrEmpty(ticimaxCustomer.FirstName)
                                ? existingCustomer.FirstName
                                : ticimaxCustomer.FirstName;
                            existingCustomer.LastName = string.IsNullOrEmpty(ticimaxCustomer.LastName)
                                ? existingCustomer.LastName
                                : ticimaxCustomer.LastName;
                            existingCustomer.Email = string.IsNullOrEmpty(ticimaxCustomer.Email)
                                ? existingCustomer.Email
                                : ticimaxCustomer.Email;
                            existingCustomer.UpdatedAt = DateTime.Now;
                            existingCustomer.LastSyncDate = DateTime.Now;

                            _logger.LogDebug("Updated customer {CustomerId} (ExternalId: {ExternalId}) birth date from Ticimax",
                                existingCustomer.Id, externalId);
                        }
                        else
                        {
                            _logger.LogDebug("Customer with ExternalId {ExternalId} not found in local database, skipping",
                                externalId);
                        }
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully synced birthday customers from Ticimax for company {CompanyId}",
                        companyId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error syncing birthday customers from Ticimax for company {CompanyId}, continuing with local data",
                companyId.Value);
        }

        var allCustomers = await _context.Customers
            .Where(c => c.CompanyId == companyId.Value &&
                       c.IsActive &&
                       c.BirthDate.HasValue)
            .ToListAsync();

        var birthdayCustomers = allCustomers
            .Select(c => new { Customer = c, LocalBirthDate = c.BirthDate!.Value })
            .Where(x => x.LocalBirthDate.Month == today.Month && x.LocalBirthDate.Day == today.Day)
            .Select(x => x.Customer)
            .ToList();

        return Json(new
        {
            success = true,
            data = birthdayCustomers,
            count = birthdayCustomers.Count
        });
    }

    /// <summary>
    /// Doğum günü bildirim gönderim geçmişini listeler
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetBirthdayNotificationHistory(int page = 1, int pageSize = 20)
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var query = _context.BirthdayNotifications
            .Include(n => n.Customer)
            .Where(n => n.CompanyId == companyId.Value)
            .OrderByDescending(n => n.SentAt);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new
            {
                n.Id,
                CustomerName = n.Customer != null 
                    ? (n.Customer.FirstName + " " + n.Customer.LastName).Trim() 
                    : "Bilinmiyor",
                CustomerEmail = n.Customer != null ? n.Customer.Email : null,
                CustomerPhone = n.Customer != null ? n.Customer.Phone : null,
                n.Channel,
                n.TemplateName,
                n.Success,
                n.ErrorMessage,
                n.Cost,
                SentAt = n.SentAt,
                n.IsAutomatic
            })
            .ToListAsync();

        return Json(new
        {
            success = true,
            data = notifications,
            pagination = new
            {
                currentPage = page,
                pageSize,
                totalCount,
                totalPages
            }
        });
    }

    /// <summary>
    /// Şirketin aktif iletişim kanallarını listeler
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetActiveNotificationChannels()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var activeChannels = await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .Where(ci => ci.CompanyId == companyId &&
                       ci.IsActive &&
                       ci.IsConfigured &&
                       ci.Integration.Type == "communication")
            .Select(ci => new
            {
                ci.Integration.Name,
                ci.Integration.Description,
                ci.Integration.IconClass,
                ci.Integration.IconColor,
                ci.IsActive,
                ci.IsConfigured,
                ci.LastSyncAt
            })
            .ToListAsync();

        return Json(new
        {
            success = true,
            data = activeChannels,
            count = activeChannels.Count
        });
    }


}
