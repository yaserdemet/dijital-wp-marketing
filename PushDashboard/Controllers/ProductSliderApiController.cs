using Microsoft.AspNetCore.Mvc;
using PushDashboard.Services;

namespace PushDashboard.Controllers;

[ApiController]
[Route("api/productslider")]
public class ProductSliderApiController : ControllerBase
{
    private readonly IProductSliderService _productSliderService;
    private readonly ILogger<ProductSliderApiController> _logger;

    public ProductSliderApiController(
        IProductSliderService productSliderService,
        ILogger<ProductSliderApiController> logger)
    {
        _productSliderService = productSliderService;
        _logger = logger;
    }

    /// <summary>
    /// DEPRECATED: Bu endpoint artık kullanılmıyor. R2 standalone script kullanın.
    /// </summary>
    [HttpGet("{companyId}/script/{sliderId}")]
    [Obsolete("Bu endpoint artık kullanılmıyor. R2 standalone script kullanın.")]
    public IActionResult GetScript(Guid companyId, int sliderId)
    {
        return Content(@"
// DEPRECATED ENDPOINT
// Bu endpoint artık kullanılmıyor.
// Lütfen yeni R2 tabanlı standalone script kullanın.
// Admin panelinden 'R2 Embed Kodu Al' butonunu kullanın.
console.warn('DEPRECATED: Bu endpoint artık kullanılmıyor. R2 standalone script kullanın.');
",
            "application/javascript");
    }

    /// <summary>
    /// Widget konfigürasyon verisini döner
    /// </summary>
    [HttpGet("{companyId}/config/{sliderId}")]
    public async Task<IActionResult> GetConfig(Guid companyId, int sliderId)
    {
        var config = await _productSliderService.GetWidgetConfigAsync(companyId, sliderId);

        if (config == null)
        {
            return NotFound(new { success = false, message = "Slider bulunamadı veya aktif değil." });
        }

        Response.Headers.Add("Cache-Control", "public, max-age=300"); // 5 dakika cache

        return Ok(new { success = true, data = config });
    }

    /// <summary>
    /// Slider verilerini döner (JSONP desteği ile)
    /// </summary>
    [HttpGet("{companyId}/data/{sliderId}")]
    public async Task<IActionResult> GetData(Guid companyId, int sliderId, [FromQuery] string? callback = null)
    {

        var config = await _productSliderService.GetWidgetConfigAsync(companyId, sliderId);

        if (config == null)
        {
            var errorResponse = new { success = false, message = "Slider bulunamadı veya aktif değil." };

            if (!string.IsNullOrEmpty(callback))
            {
                var jsonpError = $"{callback}({System.Text.Json.JsonSerializer.Serialize(errorResponse)});";
                return Content(jsonpError, "application/javascript");
            }

            return NotFound(errorResponse);
        }

        Response.Headers.Add("Cache-Control", "public, max-age=300"); // 5 dakika cache

        var response = new { success = true, data = config };

        // JSONP desteği
        if (!string.IsNullOrEmpty(callback))
        {
            var jsonpResponse = $"{callback}({System.Text.Json.JsonSerializer.Serialize(response)});";
            return Content(jsonpResponse, "application/javascript");
        }

        return Ok(response);
    }

    /// <summary>
    /// Slider performans verilerini döner
    /// </summary>
    [HttpGet("{companyId}/performance/{sliderId}")]
    public async Task<IActionResult> GetPerformance(Guid companyId, int sliderId)
    {
        var performance = await _productSliderService.GetSliderPerformanceAsync(companyId, sliderId);

        Response.Headers.Add("Cache-Control", "public, max-age=60"); // 1 dakika cache

        return Ok(new { success = true, data = performance });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.Now,
            Message = "Product Slider API çalışıyor"
        });
    }

    /// <summary>
    /// API versiyon bilgisi
    /// </summary>
    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        return Ok(new
        {
            Version = "1.0.0",
            ApiName = "Product Slider API",
            Timestamp = DateTime.Now
        });
    }
}