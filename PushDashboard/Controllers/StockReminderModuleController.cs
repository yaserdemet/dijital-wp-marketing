using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.DTOs;
using PushDashboard.Filters;
using PushDashboard.Models;
using PushDashboard.Services.Modules.StockReminder;
using PushDashboard.Services;
using System.Security.Claims;
using PushDashboard.Data;
using Microsoft.Extensions.DependencyInjection;

namespace PushDashboard.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class StockReminderModuleController : BaseController
{
    private readonly IStockReminderModuleService _stockReminderModuleService;
    private readonly ILogger<StockReminderModuleController> _logger;
    private readonly IActivityLogService _activityLogService;

    public StockReminderModuleController(
        IStockReminderModuleService stockReminderModuleService,
        ILogger<StockReminderModuleController> logger,
        IActivityLogService activityLogService,
        ApplicationDbContext context,
        IUserContextService userContextService) : base(userContextService, context)
    {
        _stockReminderModuleService = stockReminderModuleService;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Şirketin aktif iletişim kanallarını getirir
    /// </summary>
    [HttpGet("active-channels")]
    [RequireCompanyId]
    public async Task<ActionResult<List<ActiveChannelDto>>> GetActiveChannels()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var channels = await _stockReminderModuleService.GetActiveChannelsAsync(companyId);

        _logger.LogInformation("Retrieved {Count} active channels for company {CompanyId}", channels.Count, companyId);
        return Ok(channels);
    }

    /// <summary>
    /// Stok hatırlatma modülü ayarlarını getirir
    /// </summary>
    [HttpGet("settings")]
    [RequireCompanyId]
    public async Task<ActionResult<StockReminderSettingsDto>> GetSettings()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var settings = await _stockReminderModuleService.GetModuleSettingsAsync(companyId);

        _logger.LogInformation("Retrieved stock reminder module settings for company {CompanyId}", companyId);
        return Ok(settings);
    }

    /// <summary>
    /// Stok hatırlatma modülü ayarlarını günceller
    /// </summary>
    [HttpPost("settings")]
    [ValidateModelState]
    [RequireUser]
    public async Task<ActionResult> UpdateSettings([FromBody] UpdateStockReminderSettingsRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId()!;

        var (success, message) = await _stockReminderModuleService.UpdateModuleSettingsAsync(companyId, request, userId);

        if (success)
        {
            _logger.LogInformation("Stock reminder module settings updated for company {CompanyId} by user {UserId}", companyId, userId);

            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Update,
                ActivityModules.StockReminder,
                "Stok hatırlatma modülü ayarları güncellendi",
                entityType: "StockReminderSettings",
                details: new { request.Enabled, request.StockThreshold },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Ok(new { message });
        }
        else
        {
            _logger.LogWarning("Failed to update stock reminder module settings for company {CompanyId}: {Message}", companyId, message);
            return BadRequest(new { message });
        }

    }

    /// <summary>
    /// Stok hatırlatma modülü istatistiklerini getirir
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<StockReminderStatsDto>> GetStats()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var stats = await _stockReminderModuleService.GetModuleStatsAsync(companyId);

        _logger.LogInformation("Retrieved stock reminder module stats for company {CompanyId}", companyId);
        return Ok(stats);
    }

    /// <summary>
    /// Ürün stoğunun eşiğin altında olup olmadığını kontrol eder
    /// </summary>
    [HttpGet("check-stock/{productId}/{currentStock}")]
    public async Task<ActionResult<bool>> CheckStockThreshold(int productId, int currentStock)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var isBelowThreshold = await _stockReminderModuleService.IsStockBelowThresholdAsync(companyId, productId, currentStock);

        _logger.LogInformation("Stock threshold check for product {ProductId} in company {CompanyId}: {IsBelowThreshold}",
            productId,
            companyId,
            isBelowThreshold);

        return Ok(new { productId, currentStock, isBelowThreshold });
    }

    /// <summary>
    /// Stok hatırlatma bildirimi gönderir (manuel test için)
    /// </summary>
    [HttpPost("send-notification/{productId}/{currentStock}")]
    [RequireUser]
    public async Task<ActionResult> SendNotification(int productId, int currentStock)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId()!;

        var (success, message, notificationsSent, totalCost) = await _stockReminderModuleService
            .SendStockReminderNotificationAsync(companyId, productId, currentStock, userId);

        if (success)
        {
            _logger.LogInformation("Manual stock reminder notification sent for product {ProductId}. Notifications: {Count}, Cost: {Cost:C}",
                productId,
                notificationsSent,
                totalCost);

            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Send,
                ActivityModules.StockReminder,
                $"Stok hatırlatma bildirimi gönderildi (Ürün ID: {productId})",
                entityId: productId.ToString(),
                entityType: "StockReminderNotification",
                details: new { productId, currentStock, notificationsSent, totalCost },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Ok(new
            {
                message,
                notificationsSent,
                totalCost,
                success = true
            });
        }
        else
        {
            _logger.LogWarning("Failed to send manual stock reminder notification for product {ProductId}: {Message}", productId, message);
            return BadRequest(new { message, success = false });
        }

    }

    /// <summary>
    /// Tüm şirketler için e-ticaret sisteminden stok alarm ürünlerini çeker
    /// </summary>
    [HttpPost("pull-stock-reminders")]
    [AllowAnonymous]
    public IActionResult PullStockReminders()
    {
        _logger.LogInformation("Pull stock reminders requested for all companies (Background Task)");

        var serviceScopeFactory = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();

        Task.Run(async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var scopedService = scope.ServiceProvider.GetRequiredService<IStockReminderModuleService>();
            var localLogger = scope.ServiceProvider.GetRequiredService<ILogger<StockReminderModuleController>>();

            try
            {
                await scopedService.PullAllStockRemindersAsync();
                localLogger.LogInformation("Background PullAllStockRemindersAsync completed successfully.");
            }
            catch (Exception ex)
            {
                localLogger.LogError(ex, "Error during background PullAllStockRemindersAsync.");
            }
        });

        return Accepted(new { Success = true, Message = "İşlem arka planda başlatıldı." });
    }

    /// <summary>
    /// Tüm şirketler için stoğu gelen ürünlerin abonelerine mesaj gönderir (kritik stok seviyesine göre)
    /// </summary>
    [HttpPost("send-stock-messages")]
    [AllowAnonymous]
    public IActionResult SendStockMessages()
    {
        _logger.LogInformation("Send stock messages requested for all companies (Background Task)");

        var serviceScopeFactory = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();

        Task.Run(async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var scopedService = scope.ServiceProvider.GetRequiredService<IStockReminderModuleService>();
            var localLogger = scope.ServiceProvider.GetRequiredService<ILogger<StockReminderModuleController>>();

            try
            {
                await scopedService.SendAllStockReminderMessagesAsync();
                localLogger.LogInformation("Background SendAllStockReminderMessagesAsync completed successfully.");
            }
            catch (Exception ex)
            {
                localLogger.LogError(ex, "Error during background SendAllStockReminderMessagesAsync.");
            }
        });

        return Accepted(new { Success = true, Message = "İşlem arka planda başlatıldı." });
    }
}