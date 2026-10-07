using Microsoft.AspNetCore.Mvc;
using PushDashboard.Services;

namespace PushDashboard.Controllers;

/// <summary>
/// Modül abonelik yenileme API controller'ı
/// Günlük tetiklenecek yenileme kontrolü ve yönetimi
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly IModuleSubscriptionService _subscriptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        IModuleSubscriptionService subscriptionService,
        IConfiguration configuration,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Süresi dolan abonelikleri kontrol edip yeniler
    /// Günlük tetiklenecek endpoint
    /// </summary>
    [HttpPost("process-renewals")]
    public async Task<IActionResult> ProcessRenewals([FromHeader(Name = "X-Api-Key")] string? apiKey)
    {
        // API key doğrulaması
        var expectedApiKey = _configuration["SubscriptionRenewal:ApiKey"];
        if (string.IsNullOrEmpty(expectedApiKey))
        {
            _logger.LogWarning("Subscription renewal API key is not configured");
            return StatusCode(500, new { success = false, message = "API key yapılandırılmamış" });
        }

        if (string.IsNullOrEmpty(apiKey) || apiKey != expectedApiKey)
        {
            _logger.LogWarning("Invalid API key attempt for subscription renewal");
            return Unauthorized(new { success = false, message = "Geçersiz API anahtarı" });
        }

        try
        {
            _logger.LogInformation("Starting subscription renewal process");

            var results = await _subscriptionService.ProcessExpiredSubscriptionsAsync();

            var summary = new
            {
                success = true,
                totalProcessed = results.Count,
                successfulRenewals = results.Count(r => r.Success),
                failedRenewals = results.Count(r => !r.Success),
                totalAmountCharged = results.Where(r => r.Success).Sum(r => r.AmountCharged ?? 0),
                results = results.Select(r => new
                {
                    companyId = r.CompanyId,
                    moduleId = r.ModuleId,
                    moduleName = r.ModuleName,
                    success = r.Success,
                    message = r.Message,
                    amountCharged = r.AmountCharged,
                    newExpiryDate = r.NewExpiryDate?.ToString("yyyy-MM-dd")
                })
            };

            _logger.LogInformation(
                "Subscription renewal completed. Total: {Total}, Success: {Success}, Failed: {Failed}",
                summary.totalProcessed,
                summary.successfulRenewals,
                summary.failedRenewals);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing subscription renewals");
            return StatusCode(500, new { success = false, message = "Yenileme işlemi sırasında bir hata oluştu" });
        }
    }

    /// <summary>
    /// Yaklaşan süre bitimlerini listeler
    /// </summary>
    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiringSubscriptions([FromHeader(Name = "X-Api-Key")] string? apiKey, [FromQuery] int days = 3)
    {
        // API key doğrulaması
        var expectedApiKey = _configuration["SubscriptionRenewal:ApiKey"];
        if (string.IsNullOrEmpty(expectedApiKey))
        {
            return StatusCode(500, new { success = false, message = "API key yapılandırılmamış" });
        }

        if (string.IsNullOrEmpty(apiKey) || apiKey != expectedApiKey)
        {
            return Unauthorized(new { success = false, message = "Geçersiz API anahtarı" });
        }

        try
        {
            var expiringSubscriptions = await _subscriptionService.GetExpiringSubscriptionsAsync(days);

            return Ok(new
            {
                success = true,
                count = expiringSubscriptions.Count,
                subscriptions = expiringSubscriptions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiring subscriptions");
            return StatusCode(500, new { success = false, message = "İşlem sırasında bir hata oluştu" });
        }
    }
}
