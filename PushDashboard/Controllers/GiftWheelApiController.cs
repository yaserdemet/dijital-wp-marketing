using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PushDashboard.DTOs;
using PushDashboard.Services.Modules.GiftWheel;
using System.ComponentModel.DataAnnotations;

namespace PushDashboard.Controllers;

/// <summary>
/// Public API controller for Gift Wheel functionality (no authentication required)
/// </summary>
[ApiController]
[Route("api/gift-wheel")]
public class GiftWheelApiController : ControllerBase
{
    private readonly IGiftWheelService _giftWheelService;
    private readonly IGiftWheelModuleService _moduleService;
    private readonly ILogger<GiftWheelApiController> _logger;

    public GiftWheelApiController(
        IGiftWheelService giftWheelService,
        IGiftWheelModuleService moduleService,
        ILogger<GiftWheelApiController> logger)
    {
        _giftWheelService = giftWheelService;
        _moduleService = moduleService;
        _logger = logger;
    }

    /// <summary>
    /// Process a wheel spin request
    /// </summary>
    [HttpPost("{companyId}/spin")]
    [EnableRateLimiting("GiftWheelSpinPolicy")]
    public async Task<ActionResult<GiftWheelSpinResult>> ProcessSpin(
        [FromRoute] Guid companyId,
        [FromBody] GiftWheelSpinRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new GiftWheelSpinResult
                {
                    Success = false,
                    Message = string.Join(", ", errors),
                    ErrorCode = "VALIDATION_ERROR"
                });
            }

            // Get client IP address
            var ipAddress = GetClientIpAddress();

            // Validate module usage
            var (canUse, message, requiredCredits) = await _moduleService.ValidateModuleUsageAsync(companyId, "spin");
            if (!canUse)
            {
                return BadRequest(new GiftWheelSpinResult
                {
                    Success = false,
                    Message = message,
                    ErrorCode = "MODULE_NOT_AVAILABLE"
                });
            }
            
            request.CustomerPhone = request.CustomerPhone.Replace("+", "");

            // Process the spin
            var result = await _giftWheelService.ProcessSpinAsync(companyId, request, ipAddress);

            if (result.Success)
            {
                _logger.LogInformation("Successful spin processed for company {CompanyId}, customer {CustomerName}", 
                    companyId, request.CustomerName);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Failed spin attempt for company {CompanyId}, customer {CustomerName}: {Message}", 
                    companyId, request.CustomerName, result.Message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing spin for company {CompanyId}", companyId);
            return StatusCode(500, new GiftWheelSpinResult
            {
                Success = false,
                Message = "Bir hata oluştu. Lütfen tekrar deneyin.",
                ErrorCode = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// Get wheel configuration for embed script
    /// </summary>
    [HttpGet("{companyId}/config")]
    [EnableRateLimiting("GiftWheelConfigPolicy")]
    public async Task<ActionResult<GiftWheelConfigDto>> GetWheelConfig([FromRoute] Guid companyId)
    {
        try
        {
            // Check if company has active module
            if (!await _moduleService.HasActiveModuleAsync(companyId))
            {
                return NotFound(new { message = "Hediye çarkı modülü aktif değil." });
            }

            var config = await _giftWheelService.GetWheelConfigAsync(companyId);
            if (config == null)
            {
                return NotFound(new { message = "Çark konfigürasyonu bulunamadı." });
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wheel config for company {CompanyId}", companyId);
            return StatusCode(500, new { message = "Konfigürasyon alınırken hata oluştu." });
        }
    }

    /// <summary>
    /// Check if customer can spin the wheel
    /// </summary>
    [HttpGet("{companyId}/eligibility")]
    [EnableRateLimiting("GiftWheelEligibilityPolicy")]
    public async Task<ActionResult<GiftWheelEligibilityDto>> CheckEligibility(
        [FromRoute] Guid companyId,
        [FromQuery] string? customerPhone = null)
    {
        try
        {
            // Check if company has active module
            if (!await _moduleService.HasActiveModuleAsync(companyId))
            {
                return Ok(new GiftWheelEligibilityDto
                {
                    CanSpin = false,
                    Message = "Hediye çarkı modülü aktif değil.",
                    ErrorCode = "MODULE_NOT_ACTIVE"
                });
            }

            // Get client IP address
            var ipAddress = GetClientIpAddress();

            // If phone provided, check specific eligibility
            if (!string.IsNullOrEmpty(customerPhone))
            {
                var (canSpin, message) = await _giftWheelService.ValidateSpinEligibilityAsync(companyId, customerPhone, ipAddress);
                return Ok(new GiftWheelEligibilityDto
                {
                    CanSpin = canSpin,
                    Message = message,
                    ErrorCode = canSpin ? null : "SPIN_NOT_ALLOWED"
                });
            }

            // General eligibility check (wheel exists and active)
            var wheel = await _giftWheelService.GetCompanyWheelAsync(companyId);
            if (wheel == null || !wheel.IsActive)
            {
                return Ok(new GiftWheelEligibilityDto
                {
                    CanSpin = false,
                    Message = "Hediye çarkı bulunamadı veya aktif değil.",
                    ErrorCode = "WHEEL_NOT_FOUND"
                });
            }

            return Ok(new GiftWheelEligibilityDto
            {
                CanSpin = true,
                Message = "Çark kullanılabilir.",
                ErrorCode = null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking eligibility for company {CompanyId}", companyId);
            return Ok(new GiftWheelEligibilityDto
            {
                CanSpin = false,
                Message = "Doğrulama sırasında hata oluştu.",
                ErrorCode = "INTERNAL_ERROR"
            });
        }
    }

    /// <summary>
    /// Get JavaScript embed script for the wheel
    /// </summary>
    [HttpGet("{companyId}/script")]
    [EnableRateLimiting("GiftWheelScriptPolicy")]
    public async Task<ActionResult> GetEmbedScript([FromRoute] Guid companyId)
    {
        try
        {
            // Check if company has active module
            if (!await _moduleService.HasActiveModuleAsync(companyId))
            {
                return NotFound("// Hediye çarkı modülü aktif değil.");
            }

            var script = await _giftWheelService.GenerateEmbedScriptAsync(companyId);
            
            return Content(script, "application/javascript");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embed script for company {CompanyId}", companyId);
            return Content("// Script oluşturulurken hata oluştu.", "application/javascript");
        }
    }

    /// <summary>
    /// Check if customer can spin the wheel (for pre-validation)
    /// </summary>
    [HttpPost("{companyId}/validate-spin")]
    [EnableRateLimiting("GiftWheelValidationPolicy")]
    public async Task<ActionResult> ValidateSpinEligibility(
        [FromRoute] Guid companyId,
        [FromBody] SpinValidationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { canSpin = false, message = "Geçersiz istek." });
            }

            var ipAddress = GetClientIpAddress();
            var (canSpin, message) = await _giftWheelService.ValidateSpinEligibilityAsync(companyId, request.CustomerPhone, ipAddress);

            return Ok(new { canSpin, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating spin eligibility for company {CompanyId}", companyId);
            return StatusCode(500, new { canSpin = false, message = "Doğrulama sırasında hata oluştu." });
        }
    }

    /// <summary>
    /// Health check endpoint for the gift wheel service
    /// </summary>
    [HttpGet("{companyId}/health")]
    public async Task<ActionResult> HealthCheck([FromRoute] Guid companyId)
    {
        try
        {
            var hasActiveModule = await _moduleService.HasActiveModuleAsync(companyId);
            var integrationStatus = await _moduleService.CheckRequiredIntegrationsAsync(companyId);

            var health = new
            {
                status = hasActiveModule ? "healthy" : "inactive",
                moduleActive = hasActiveModule,
                ecommerceIntegration = integrationStatus.HasEcommerceIntegration && integrationStatus.IsEcommerceConfigured,
                whatsappIntegration = integrationStatus.HasWhatsAppIntegration && integrationStatus.IsWhatsAppConfigured,
                timestamp = DateTime.Now
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for company {CompanyId}", companyId);
            return StatusCode(500, new { status = "error", message = "Sağlık kontrolü başarısız." });
        }
    }

    #region Helper Methods

    /// <summary>
    /// Gets the client IP address from the request
    /// </summary>
    private string GetClientIpAddress()
    {
        try
        {
            // Check for forwarded IP first (in case of proxy/load balancer)
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for real IP header
            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to connection remote IP
            return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    #endregion
}

/// <summary>
/// Request model for spin validation
/// </summary>
public class SpinValidationRequest
{
    [Required(ErrorMessage = "Telefon numarası gereklidir")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    public string CustomerPhone { get; set; } = string.Empty;
}

/// <summary>
/// Rate limiting policies for gift wheel API
/// </summary>
public static class GiftWheelRateLimitPolicies
{
    public const string SpinPolicy = "GiftWheelSpinPolicy";
    public const string ConfigPolicy = "GiftWheelConfigPolicy";
    public const string ScriptPolicy = "GiftWheelScriptPolicy";
    public const string ValidationPolicy = "GiftWheelValidationPolicy";
}
