using Microsoft.AspNetCore.Mvc;
using PushDashboard.Services;
using PushDashboard.Services.Modules.Birthday;
using PushDashboard.ViewModels;

namespace PushDashboard.Controllers.Api;

[ApiController]
[Route("api/basket-reminder")]
public class BasketReminderApiController : ControllerBase
{
    private readonly IBasketReminderService _basketReminderService;
    private readonly ILogger<BasketReminderApiController> _logger;
    private readonly IBirthdayModuleService _birthdayModuleService;

    public BasketReminderApiController(
        IBasketReminderService basketReminderService,
        ILogger<BasketReminderApiController> logger,
        IBirthdayModuleService birthdayModuleService)
    {
        _basketReminderService = basketReminderService;
        _logger = logger;
        _birthdayModuleService = birthdayModuleService;
    }

    /// <summary>
    /// Tüm şirketler için sepet hatırlatmalarını işler (Hangfire tarafından çağrılır)
    /// </summary>
    [HttpPost("process-all")]
    public async Task<ActionResult<ProcessRemindersResultViewModel>> ProcessAllReminders()
    {
        try
        {
            _logger.LogInformation("API call received to process all basket reminders");

            var (success, message, processedCompanies, totalReminders) = await _basketReminderService.ProcessAllRemindersAsync();

            var result = new ProcessRemindersResultViewModel
            {
                Success = success,
                Message = message,
                ProcessedCount = totalReminders,
                SuccessfulCount = success ? totalReminders : 0,
                FailedCount = success ? 0 : totalReminders
            };

            if (success)
            {
                _logger.LogInformation("Successfully processed basket reminders for {ProcessedCompanies} companies, {TotalReminders} total reminders", 
                    processedCompanies, totalReminders);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Failed to process basket reminders: {Message}", message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing all basket reminders via API");
            
            var errorResult = new ProcessRemindersResultViewModel
            {
                Success = false,
                Message = "API çağrısı sırasında hata oluştu: " + ex.Message,
                ProcessedCount = 0,
                SuccessfulCount = 0,
                FailedCount = 0,
                Errors = new List<string> { ex.Message }
            };

            return StatusCode(500, errorResult);
        }
    }

    /// <summary>
    /// Belirli bir şirket için sepet hatırlatmalarını işler
    /// </summary>
    [HttpPost("process-company/{companyId:guid}")]
    public async Task<ActionResult<ProcessRemindersResultViewModel>> ProcessCompanyReminders(Guid companyId)
    {
        try
        {
            _logger.LogInformation("API call received to process basket reminders for company {CompanyId}", companyId);

            var (success, message, processedCount) = await _basketReminderService.ProcessRemindersForCompanyAsync(companyId);

            var result = new ProcessRemindersResultViewModel
            {
                Success = success,
                Message = message,
                ProcessedCount = processedCount,
                SuccessfulCount = success ? processedCount : 0,
                FailedCount = success ? 0 : processedCount
            };

            if (success)
            {
                _logger.LogInformation("Successfully processed {ProcessedCount} basket reminders for company {CompanyId}", 
                    processedCount, companyId);
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("Failed to process basket reminders for company {CompanyId}: {Message}", companyId, message);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing basket reminders for company {CompanyId} via API", companyId);
            
            var errorResult = new ProcessRemindersResultViewModel
            {
                Success = false,
                Message = "API çağrısı sırasında hata oluştu: " + ex.Message,
                ProcessedCount = 0,
                SuccessfulCount = 0,
                FailedCount = 0,
                Errors = new List<string> { ex.Message }
            };

            return StatusCode(500, errorResult);
        }
    }
    
    /// <summary>
    /// Tüm şirketler için doğum günü bildirimleri gönderir (Admin endpoint)
    /// </summary>
    [HttpPost("birthday/process-all")]
    public async Task<IActionResult> SendBirthdayNotificationsForAllCompanies()
    {
        var result = await _birthdayModuleService.SendBirthdayNotificationsForAllCompaniesAsync();

        return Ok(new
        {
            success = result.Success,
            message = result.Message,
            notificationsSent = result.NotificationsSent
        });
    }

    /// <summary>
    /// Sepet hatırlatma modülü durumunu kontrol eder
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult> HealthCheck()
    {
        try
        {
            // Basit bir health check - database bağlantısını test et
            var testResult = await _basketReminderService.ProcessAllRemindersAsync();
            
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.Now,
                Message = "Sepet hatırlatma servisi çalışıyor"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for basket reminder service");
            
            return StatusCode(500, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.Now,
                Message = "Sepet hatırlatma servisi hatası: " + ex.Message
            });
        }
    }

    /// <summary>
    /// Belirli bir şirketin e-ticaret entegrasyonunu doğrular
    /// </summary>
    [HttpGet("validate-integration/{companyId:guid}")]
    public async Task<ActionResult> ValidateEcommerceIntegration(Guid companyId)
    {
        try
        {
            var (isValid, message) = await _basketReminderService.ValidateEcommerceIntegrationAsync(companyId);

            return Ok(new
            {
                IsValid = isValid,
                Message = message,
                CompanyId = companyId,
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ecommerce integration for company {CompanyId}", companyId);
            
            return StatusCode(500, new
            {
                IsValid = false,
                Message = "Entegrasyon doğrulama sırasında hata oluştu: " + ex.Message,
                CompanyId = companyId,
                Timestamp = DateTime.Now
            });
        }
    }
}
