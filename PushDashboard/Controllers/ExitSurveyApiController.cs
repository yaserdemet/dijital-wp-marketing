using Microsoft.AspNetCore.Mvc;
using PushDashboard.Services;
using PushDashboard.Models.ViewModels;
using System.Text.Json;

namespace PushDashboard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExitSurveyApiController : ControllerBase
    {
        private readonly IExitSurveyService _exitSurveyService;
        private readonly ILogger<ExitSurveyApiController> _logger;

        public ExitSurveyApiController(IExitSurveyService exitSurveyService, ILogger<ExitSurveyApiController> logger)
        {
            _exitSurveyService = exitSurveyService;
            _logger = logger;
        }

        /// <summary>
        /// Widget script'ini döndürür
        /// </summary>
        [HttpGet("widget/{companyId}")]
        public async Task<IActionResult> GetWidget(Guid companyId)
        {
            try
            {
                var hasModule = await _exitSurveyService.HasExitSurveyModuleAsync(companyId);
                if (!hasModule)
                {
                    return NotFound("// Exit survey module not found");
                }

                var config = await _exitSurveyService.GetScriptConfigAsync(companyId);

                var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var scriptContent = GenerateJavaScriptWidget(configJson, companyId);

                Response.Headers["Cache-Control"] = "public, max-age=300";
                Response.Headers["Content-Type"] = "application/javascript; charset=utf-8";
                Response.Headers["X-Content-Type-Options"] = "nosniff";

                return Content(scriptContent, "application/javascript");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating widget script for company {CompanyId}", companyId);
                return StatusCode(500, "// Error generating widget script");
            }
        }

        /// <summary>
        /// Anket cevaplarını alır
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitResponse([FromBody] SubmitExitSurveyResponseViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Geçersiz veri" });
                }

                // IP address ve user agent bilgilerini al
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var success = await _exitSurveyService.SubmitResponseAsync(model, ipAddress, userAgent);

                if (success)
                {
                    return Ok(new { success = true, message = "Cevaplarınız kaydedildi. Teşekkür ederiz!" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Cevaplar kaydedilirken bir hata oluştu" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting exit survey response");
                return StatusCode(500, new { success = false, message = "Sunucu hatası" });
            }
        }

        [HttpPost("track")]
        public async Task<IActionResult> Track([FromBody] ExitSurveyTrackingViewModel model)
        {
            try
            {
                if (model.EventType != "impression" && model.EventType != "submit")
                {
                    return BadRequest(new { success = false, message = "Geçersiz event type" });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var success = await _exitSurveyService.RecordTrackingEventAsync(model, ipAddress, userAgent);

                return success
                    ? Ok(new { success = true })
                    : BadRequest(new { success = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking exit survey event");
                return StatusCode(500, new { success = false });
            }
        }

        private string GenerateJavaScriptWidget(string configJson, Guid companyId)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var script = $@"
(function() {{
    'use strict';
    
    var cssLink = document.createElement('link');
    cssLink.rel = 'stylesheet';
    cssLink.href = '{baseUrl}/css/exit-survey-widget.css';
    document.head.appendChild(cssLink);
    
    var s = document.createElement('script');
    s.src = '{baseUrl}/js/modules/exit-survey-module.js';
    s.onload = function() {{
        var config = {configJson};
        config.submitUrl = '{baseUrl}/api/ExitSurveyApi/submit';
        config.trackUrl = '{baseUrl}/api/ExitSurveyApi/track';
        if (window.initExitSurvey) {{
            window.initExitSurvey(config);
        }}
    }};
    document.head.appendChild(s);
}})();";

            return script;
        }
    }
}
