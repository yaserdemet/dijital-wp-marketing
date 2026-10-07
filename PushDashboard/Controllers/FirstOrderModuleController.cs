using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.DTOs;
using PushDashboard.Models;
using PushDashboard.Services.Modules.FirstOrder;
using PushDashboard.Services;
using System.Security.Claims;
using PushDashboard.Data;

namespace PushDashboard.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class FirstOrderModuleController : BaseController
{
    private readonly IFirstOrderModuleService _firstOrderModuleService;
    private readonly ILogger<FirstOrderModuleController> _logger;
    private readonly IActivityLogService _activityLogService;

    public FirstOrderModuleController(
        IFirstOrderModuleService firstOrderModuleService,
        ILogger<FirstOrderModuleController> logger,
        IActivityLogService activityLogService,
        IUserContextService userContextService, 
        ApplicationDbContext context) : base(userContextService, context)
    {
        _firstOrderModuleService = firstOrderModuleService;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Şirketin aktif iletişim kanallarını getirir
    /// </summary>
    [HttpGet("active-channels")]
    public async Task<ActionResult<List<ActiveChannelDto>>> GetActiveChannels()
    {
        try
        {
            var companyId = GetCurrentUserCompanyId()!.Value;
            var channels = await _firstOrderModuleService.GetActiveChannelsAsync(companyId);
            
            _logger.LogInformation("Retrieved {Count} active channels for company {CompanyId}", channels.Count, companyId);
            return Ok(channels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active channels");
            return StatusCode(500, new { message = "Aktif kanallar alınırken hata oluştu." });
        }
    }

    /// <summary>
    /// İlk alışveriş modülü ayarlarını getirir
    /// </summary>
    [HttpGet("settings")]
    public async Task<ActionResult<FirstOrderSettingsDto>> GetSettings()
    {
        try
        {
            var companyId = GetCurrentUserCompanyId()!.Value;
            var settings = await _firstOrderModuleService.GetModuleSettingsAsync(companyId);
            
            _logger.LogInformation("Retrieved first order module settings for company {CompanyId}", companyId);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module settings");
            return StatusCode(500, new { message = "Modül ayarları alınırken hata oluştu." });
        }
    }

    /// <summary>
    /// İlk alışveriş modülü ayarlarını günceller
    /// </summary>
    [HttpPost("settings")]
    public async Task<ActionResult> UpdateSettings([FromBody] UpdateFirstOrderSettingsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var companyId = GetCurrentUserCompanyId()!.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

            var (success, message) = await _firstOrderModuleService.UpdateModuleSettingsAsync(companyId, request, userId);

            if (success)
            {
                _logger.LogInformation("First order module settings updated for company {CompanyId} by user {UserId}", companyId, userId);

                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId,
                    ActivityActionTypes.Update,
                    ActivityModules.FirstOrder,
                    "İlk alışveriş modülü ayarları güncellendi",
                    entityType: "FirstOrderSettings",
                    details: new { request.Enabled, request.GiftVoucherEnabled, request.GiftVoucherAmount },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());

                return Ok(new { message });
            }
            else
            {
                _logger.LogWarning("Failed to update first order module settings for company {CompanyId}: {Message}", companyId, message);
                return BadRequest(new { message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module settings");
            return StatusCode(500, new { message = "Ayarlar güncellenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Hediye çeki oluşturmayı test eder
    /// </summary>
    [HttpPost("test-gift-voucher")]
    public async Task<ActionResult<GiftVoucherTestResult>> TestGiftVoucher()
    {
        try
        {
            var companyId = GetCurrentUserCompanyId()!.Value;
            var result = await _firstOrderModuleService.TestGiftVoucherCreationAsync(companyId);
            
            _logger.LogInformation("Gift voucher test completed for company {CompanyId}. Success: {Success}", companyId, result.Success);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing gift voucher creation");
            return StatusCode(500, new { message = "Hediye çeki testi sırasında hata oluştu." });
        }
    }

    /// <summary>
    /// İlk alışveriş modülü istatistiklerini getirir
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<FirstOrderStatsDto>> GetStats()
    {
        try
        {
            var companyId = GetCurrentUserCompanyId()!.Value;
            var stats = await _firstOrderModuleService.GetModuleStatsAsync(companyId);
            
            _logger.LogInformation("Retrieved first order module stats for company {CompanyId}", companyId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module stats");
            return StatusCode(500, new { message = "Modül istatistikleri alınırken hata oluştu." });
        }
    }

    /// <summary>
    /// Müşterinin ilk siparişi olup olmadığını kontrol eder
    /// </summary>
    [HttpGet("check-first-order/{customerId}")]
    public async Task<ActionResult<bool>> CheckFirstOrder(int customerId)
    {
        try
        {
            var companyId = GetCurrentUserCompanyId()!.Value;
            var isFirstOrder = await _firstOrderModuleService.IsFirstOrderAsync(companyId, customerId);
            
            _logger.LogInformation("First order check for customer {CustomerId} in company {CompanyId}: {IsFirstOrder}", customerId, companyId, isFirstOrder);
            return Ok(new { isFirstOrder, customerId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking first order for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "İlk sipariş kontrolü sırasında hata oluştu." });
        }
    }

    /// <summary>
    /// İlk alışveriş bildirimi gönderir (manuel test için)
    /// </summary>
    [HttpPost("send-notification/{customerId}")]
    public async Task<ActionResult> SendNotification(int customerId)
    {
        try
        {
            var companyId = GetCurrentUserCompanyId()!.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "manual";

            var (success, message, notificationsSent, totalCost) = await _firstOrderModuleService
                .SendFirstOrderNotificationAsync(companyId, customerId, userId);

            if (success)
            {
                _logger.LogInformation("Manual first order notification sent for customer {CustomerId}. Notifications: {Count}, Cost: {Cost:C}", 
                    customerId, notificationsSent, totalCost);

                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId,
                    ActivityActionTypes.Send,
                    ActivityModules.FirstOrder,
                    $"İlk alışveriş bildirimi gönderildi (Müşteri ID: {customerId})",
                    entityId: customerId.ToString(),
                    entityType: "FirstOrderNotification",
                    details: new { customerId, notificationsSent, totalCost },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());

                return Ok(new { 
                    message, 
                    notificationsSent, 
                    totalCost,
                    success = true 
                });
            }
            else
            {
                _logger.LogWarning("Failed to send manual first order notification for customer {CustomerId}: {Message}", customerId, message);
                return BadRequest(new { message, success = false });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending manual first order notification for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Bildirim gönderilirken hata oluştu." });
        }
    }
}
