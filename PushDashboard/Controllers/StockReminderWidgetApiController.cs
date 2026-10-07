using Microsoft.AspNetCore.Mvc;
using PushDashboard.Services;

namespace PushDashboard.Controllers;

/// <summary>
/// Stok hatırlatma widget public API (müşteri ön yüzünden çağrılır, auth gerektirmez)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StockReminderWidgetApiController : ControllerBase
{
    private readonly IStockReminderWidgetService _widgetService;
    private readonly ILogger<StockReminderWidgetApiController> _logger;

    public StockReminderWidgetApiController(
        IStockReminderWidgetService widgetService,
        ILogger<StockReminderWidgetApiController> logger)
    {
        _widgetService = widgetService;
        _logger = logger;
    }

    /// <summary>
    /// Şirkete ait widget konfigürasyonunu döner (JS tarafından çalışma zamanında yüklenebilir)
    /// </summary>
    [HttpGet("config/{companyId}")]
    public async Task<IActionResult> GetConfig(Guid companyId)
    {
        try
        {
            var config = await _widgetService.GetScriptConfigAsync(companyId);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock reminder widget config for company {CompanyId}", companyId);
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// Müşteri form gönderimini kaydeder (stok hatırlatma isteği)
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] StockReminderWidgetSubmissionModel model)
    {
        try
        {
            if (model.CompanyId == Guid.Empty)
                return BadRequest(new { success = false, message = "Geçersiz şirket." });

            if (string.IsNullOrWhiteSpace(model.Phone) && string.IsNullOrWhiteSpace(model.Email))
                return BadRequest(new { success = false, message = "Telefon numarası veya e-posta adresi gereklidir." });

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var success = await _widgetService.RecordSubmissionAsync(model, ipAddress, userAgent);

            if (success)
                return Ok(new { success = true, message = "Kaydınız alındı! Ürün stoğa girdiğinde size haber vereceğiz." });

            return BadRequest(new { success = false, message = "Kayıt sırasında bir hata oluştu. Lütfen tekrar deneyin." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting stock reminder widget form");
            return StatusCode(500, new { success = false, message = "Sunucu hatası" });
        }
    }

    /// <summary>
    /// Impression / dismiss olaylarını izleme kaydı
    /// </summary>
    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] StockReminderWidgetTrackModel model)
    {
        if (model.CompanyId == Guid.Empty)
            return BadRequest(new { success = false });

        _logger.LogInformation("Stock reminder widget event: {EventType} for company {CompanyId}", model.EventType, model.CompanyId);
        await _widgetService.TrackEventAsync(model.CompanyId, model.EventType);
        return Ok(new { success = true });
    }
}

public class StockReminderWidgetTrackModel
{
    public Guid CompanyId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public string? SessionId { get; set; }
}
