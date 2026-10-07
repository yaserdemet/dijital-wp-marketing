using Microsoft.AspNetCore.Mvc;
using PushDashboard.Services;
using System.Text.Json;

namespace PushDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsletterPopupApiController : ControllerBase
{
    private readonly INewsletterPopupService _newsletterPopupService;
    private readonly ILogger<NewsletterPopupApiController> _logger;

    public NewsletterPopupApiController(
        INewsletterPopupService newsletterPopupService,
        ILogger<NewsletterPopupApiController> logger)
    {
        _newsletterPopupService = newsletterPopupService;
        _logger = logger;
    }

    /// <summary>
    /// Tracking olaylarını kaydeder (impression, submit, dismiss)
    /// </summary>
    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] NewsletterPopupTrackingModel model)
    {
        try
        {
            if (model.EventType != "impression" && model.EventType != "submit" && model.EventType != "dismiss")
            {
                return BadRequest(new { success = false, message = "Geçersiz event type" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var success = await _newsletterPopupService.RecordTrackingEventAsync(model, ipAddress, userAgent);

            return success
                ? Ok(new { success = true })
                : BadRequest(new { success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking newsletter popup event");
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// E-bülten form bilgilerini alır
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] NewsletterPopupTrackingModel model)
    {
        try
        {
            model.EventType = "submit";

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var success = await _newsletterPopupService.RecordTrackingEventAsync(model, ipAddress, userAgent);

            if (success)
            {
                return Ok(new { success = true, message = "Kaydınız başarıyla alınmıştır. Teşekkür ederiz!" });
            }
            else
            {
                return BadRequest(new { success = false, message = "Kayıt sırasında bir hata oluştu." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting newsletter popup form");
            return StatusCode(500, new { success = false, message = "Sunucu hatası" });
        }
    }
}
