using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.Services.WhatsApp;
using PushDashboard.ViewModels;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class WhatsAppTemplateController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppTemplateService _whatsAppTemplateService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IWhatsAppConversionService _conversionService;
    private readonly IWhatsAppTemplateSampleService _sampleService;
    private readonly ILogger<WhatsAppTemplateController> _logger;
    private readonly IActivityLogService _activityLogService;
    private readonly IR2StorageService _r2StorageService;

    public WhatsAppTemplateController(
        ApplicationDbContext context,
        IWhatsAppTemplateService whatsAppTemplateService,
        IWhatsAppTemplateSampleService sampleService,
        ILogger<WhatsAppTemplateController> logger,
        IActivityLogService activityLogService,
        IUserContextService userContextService, 
        IWhatsAppService whatsAppService,
        IWhatsAppConversionService conversionService,
        IR2StorageService r2StorageService)
        : base(userContextService, context)
    {
        _context = context;
        _whatsAppTemplateService = whatsAppTemplateService;
        _sampleService = sampleService;
        _logger = logger;
        _activityLogService = activityLogService;
        _whatsAppService = whatsAppService;
        _conversionService = conversionService;
        _r2StorageService = r2StorageService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction("Index", "Home");
        }

        // Check if WhatsApp is configured
        var hasWhatsAppConfig = await _whatsAppTemplateService.HasWhatsAppConfigurationAsync(user.Company.Id);
        if (!hasWhatsAppConfig)
        {
            TempData["ErrorMessage"] = "WhatsApp şablonlarını kullanabilmek için önce WhatsApp entegrasyonunu yapılandırmanız gerekiyor.";
            return RedirectToAction("Index", "Integration");
        }

        // Get grouped templates from Facebook
        var groupedTemplates = await _whatsAppTemplateService.GetGroupedFacebookTemplatesAsync(user.Company.Id);
        
        var allTemplates = groupedTemplates.Values.SelectMany(x => x).ToList();
        
        var sampleTemplates = _sampleService.GetSampleTemplates();

        var viewModel = new WhatsAppTemplateIndexViewModel
        {
            GroupedFacebookTemplates = groupedTemplates,
            AllTemplates = allTemplates,
            SampleTemplates = sampleTemplates,
            CompanyId = user.Company.Id
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplate(string templateId)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var facebookTemplate = await _whatsAppTemplateService.GetFacebookTemplateAsync(user.Company.Id, templateId);
        if (facebookTemplate == null)
        {
            return Json(ApiResponse.Error("Şablon bulunamadı."));
        }

        var response = new
        {
            success = true,
            template = new
            {
                id = facebookTemplate.Id,
                name = facebookTemplate.Name,
                category = facebookTemplate.Category,
                status = facebookTemplate.Status,
                language = facebookTemplate.Language,
                components = facebookTemplate.Components,
            }
        };

        return Json(response);
    }

    [HttpGet]
    public IActionResult GetModuleVariables(string moduleType)
    {
        Dictionary<string, string> variables;
        if (string.IsNullOrEmpty(moduleType))
        {
            variables = TemplateVariableHelper.GetAllVariables();
        }
        else
        {
            variables = TemplateVariableHelper.GetVariablesForModule(moduleType);
        }
        return Json(new { success = true, variables = variables.Select(v => new { key = v.Key, label = v.Value }) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateWhatsAppTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        // Debug: Log incoming request
        _logger.LogInformation("CreateTemplate request received: {@Request}", request);
        _logger.LogInformation("Request Name: {Name}, Category: {Category}, Language: {Language}",
            request.Name, request.Category, request.Language);
        _logger.LogInformation("Components count: {Count}", request.Components?.Count ?? 0);

        if (request.Components != null)
        {
            for (int i = 0; i < request.Components.Count; i++)
            {
                var comp = request.Components[i];
                _logger.LogInformation("Component {Index}: Type={Type}, Text={Text}, Format={Format}, ButtonsCount={ButtonsCount}",
                    i, comp.Type, comp.Text, comp.Format, comp.Buttons?.Count ?? 0);
            }
        }

        // Generate tracking GUID for conversion tracking
        var trackingGuid = Guid.NewGuid();
        
        // Append tracking GUID to URL-type button URLs
        if (request.Components != null)
        {
            var buttonsComponent = request.Components.FirstOrDefault(c => c.Type == "BUTTONS");
            if (buttonsComponent?.Buttons != null)
            {
                foreach (var button in buttonsComponent.Buttons)
                {
                    if (button.Type == "URL" && !string.IsNullOrEmpty(button.Url))
                    {
                        button.Url = AppendTrackingParameter(button.Url, trackingGuid);
                        _logger.LogInformation("Appended tracking GUID to button URL: {Url}", button.Url);
                    }
                }
            }
        }

        var createRequest = new CreateTemplateRequest
        {
            Name = request.Name,
            Category = request.Category,
            Language = request.Language,
            Components = request.Components
        };

        var createdTemplate = await _whatsAppTemplateService.CreateFacebookTemplateAsync(user.Company.Id, createRequest);

        if (createdTemplate != null)
        {
            // Save variable mappings if provided
            if (request.VariableMappings != null && request.VariableMappings.Any())
            {
                try
                {
                    // Save mappings using template NAME
                    await _whatsAppTemplateService.SaveVariableMappingsAsync(
                        user.Company.Id,
                        createdTemplate.Name,  // Use NAME instead of ID
                        request.VariableMappings);
                    
                    _logger.LogInformation("Successfully saved variable mappings for template {TemplateName}", request.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving variable mappings for template {TemplateName}", request.Name);
                    // Don't fail the overall operation if mapping save fails
                }
            }
            
            // Save media URL for media templates (IMAGE/VIDEO/DOCUMENT)
            if (!string.IsNullOrEmpty(request.MediaUrl))
            {
                try
                {
                    // Store media URL as a special variable mapping for later use when sending
                    _context.WhatsAppTemplateVariableMappings.Add(new Models.WhatsAppTemplateVariableMapping
                    {
                        CompanyId = user.Company.Id,
                        WhatsAppTemplateName = createdTemplate.Name,
                        ComponentType = "HEADER",
                        VariablePosition = 0,
                        VariableType = "media_url",
                        VariableLabel = request.MediaUrl,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Saved media URL for template {TemplateName}: {MediaUrl}", 
                        createdTemplate.Name, request.MediaUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving media URL for template {TemplateName}", request.Name);
                }
            }
            
            // Save module type and tracking GUID if provided
            if (!string.IsNullOrEmpty(request.ModuleType))
            {
                try
                {
                    var templateSettings = new WhatsAppTemplateSettings
                    {
                        CompanyId = user.Company.Id,
                        TemplateName = createdTemplate.Name,  // Use NAME
                        ModuleType = request.ModuleType,
                        TrackingGuid = trackingGuid,
                        CreatedAt = DateTime.Now
                    };
                    
                    _context.WhatsAppTemplateSettings.Add(templateSettings);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation(
                        "Successfully saved module type {ModuleType} with tracking GUID {TrackingGuid} for template {TemplateName}",
                        request.ModuleType, trackingGuid, request.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving module type for template {TemplateName}", request.Name);
                    // Don't fail if module type save fails
                }
            }
            
            // Enable button click analytics if template has buttons
            var hasButtons = request.Components?.Any(c => c.Type == "BUTTONS" && c.Buttons != null && c.Buttons.Any()) == true;
            if (hasButtons && !string.IsNullOrEmpty(createdTemplate.Id))
            {
                try
                {
                    var settings = await GetWhatsAppSettingsAsync();
                    if (settings != null)
                    {
                        var analyticsResult = await _whatsAppService.EnableButtonClickAnalyticsAsync(
                            settings.AppId,
                            settings.Token,
                            createdTemplate.Id);
                        
                        if (analyticsResult.Success)
                        {
                            _logger.LogInformation("Successfully enabled button click analytics for template {TemplateId}", createdTemplate.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to enable button click analytics for template {TemplateId}: {Error}", 
                                createdTemplate.Id, analyticsResult.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enabling button click analytics for template {TemplateId}", createdTemplate.Id);
                    // Don't fail the overall operation if button analytics fails
                }
            }
            
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.Company.Id,
                ActivityActionTypes.Create,
                ActivityModules.WhatsAppTemplate,
                $"Yeni WhatsApp şablonu oluşturuldu: {request.Name}",
                entityType: "WhatsAppTemplate",
                details: new { request.Name, request.Category, request.Language },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new {
                success = true,
                message = "WhatsApp şablonu başarıyla oluşturuldu.",
                template = createdTemplate
            });
        }
        else
        {
            return Json(ApiResponse.Error("Şablon oluşturulurken hata oluştu."));
        }
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate([FromBody] DeleteWhatsAppTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        _logger.LogInformation("Attempting to delete WhatsApp template {TemplateName} for company {CompanyId}",
            request.TemplateName, user.Company.Id);
        var success = await _whatsAppTemplateService.DeleteFacebookTemplateAsync(user.Company.Id, request.TemplateName);

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.Company.Id,
                ActivityActionTypes.Delete,
                ActivityModules.WhatsAppTemplate,
                $"WhatsApp şablonu silindi: {request.TemplateName}",
                entityType: "WhatsAppTemplate",
                details: new { TemplateName = request.TemplateName },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SuccessMessage("WhatsApp şablonu başarıyla silindi."));
        }
        else
        {
            return Json(ApiResponse.Error("Şablon silinirken hata oluştu."));
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadMedia(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Json(ApiResponse.Error("Dosya seçilmedi."));
        }

        // Validate file type and size
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "video/mp4", "application/pdf" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            return Json(ApiResponse.Error("Desteklenmeyen dosya türü."));
        }

        // Size limits (WhatsApp requirements)
        var maxSize = file.ContentType.StartsWith("image/") ? 5 * 1024 * 1024 : // 5MB for images
                        file.ContentType.StartsWith("video/") ? 16 * 1024 * 1024 : // 16MB for videos
                        100 * 1024 * 1024; // 100MB for documents

        if (file.Length > maxSize)
        {
            return Json(ApiResponse.Error("Dosya boyutu çok büyük."));
        }

        // Get current user company for file path
        var user = await GetCurrentUserWithCompanyAsync();
        if (user?.Company == null)
        {
             return Json(ApiResponse.CompanyNotFound);
        }

        try 
        {
            string publicUrl;
            string key = $"whatsapp-templates/{user.Company.Id}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            using var stream = file.OpenReadStream();
            
            if (file.ContentType.StartsWith("image/"))
            {
                publicUrl = await _r2StorageService.UploadImageAsync(key, stream, file.ContentType);
            }
            else if (file.ContentType.StartsWith("video/"))
            {
                publicUrl = await _r2StorageService.UploadVideoAsync(key, stream, file.ContentType);
            }
            else
            {
                publicUrl = await _r2StorageService.UploadFileAsync(key, stream, file.ContentType);
            }

            return Json(new {
                success = true,
                mediaId = publicUrl, // Return URL as mediaId
                fileName = file.FileName,
                size = file.Length,
                type = file.ContentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to R2");
            return Json(ApiResponse.Error("Medya yüklenirken hata oluştu."));
        }
    }

    [HttpGet]
    public IActionResult GetSampleTemplates()
    {
        var samples = _sampleService.GetSampleTemplates();
        return Json(new { success = true, templates = samples });
    }

    [HttpGet]
    public IActionResult GetSampleTemplate(string id)
    {
        var sample = _sampleService.GetSampleTemplate(id);
        if (sample == null)
        {
            return Json(ApiResponse.Error("Şablon bulunamadı."));
        }

        return Json(new { success = true, template = sample });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromSample([FromBody] CreateFromSampleRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();
        var userId = user?.Id;

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var sample = _sampleService.GetSampleTemplate(request.SampleId);
        if (sample == null)
        {
            return Json(ApiResponse.Error("Hazır şablon bulunamadı."));
        }

        var createRequest = new CreateTemplateRequest
        {
            Name = request.TemplateName,
            Category = sample.Category,
            Language = sample.Language,
            Components = sample.Components
        };

        var createdTemplate = await _whatsAppTemplateService.CreateFacebookTemplateAsync(user.Company.Id, createRequest);

        if (createdTemplate != null)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                user.Company.Id,
                ActivityActionTypes.Create,
                ActivityModules.WhatsAppTemplate,
                $"Hazır WhatsApp şablonundan oluşturuldu: {request.TemplateName}",
                entityType: "WhatsAppTemplate",
                details: new { request.SampleId, request.TemplateName },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(ApiResponse.SuccessMessage("Şablon başarıyla oluşturuldu ve onay için gönderildi."));
    }
    
     /// <summary>
    /// Template analytics verilerini Gupshup API'den getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTemplateAnalytics(string templateId, DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var settings = await GetWhatsAppSettingsAsync();
            if (settings == null)
            {
                return Json(new { success = false, message = "WhatsApp bağlantısı yapılandırılmamış." });
            }

            var result = await _whatsAppService.GetTemplateAnalyticsAsync(settings, templateId, startDate, endDate);

            if (result.Success && result.Data != null)
            {
                return Json(new
                {
                    success = true,
                    templates = result.Data.Templates.Select(t => new
                    {
                        templateId = t.TemplateId,
                        sent = t.Sent,
                        delivered = t.Delivered,
                        read = t.Read,
                        failed = t.Failed,
                        deliveryRate = t.DeliveryRate,
                        readRate = t.ReadRate,
                        buttonClicks = t.ButtonClicks?.Select(b => new
                        {
                            buttonText = b.ButtonText,
                            clicks = b.Clicks,
                            type = b.Type
                        })
                    }),
                    totals = new
                    {
                        totalSent = result.Data.Templates.Sum(t => t.Sent),
                        totalDelivered = result.Data.Templates.Sum(t => t.Delivered),
                        totalRead = result.Data.Templates.Sum(t => t.Read),
                        totalFailed = result.Data.Templates.Sum(t => t.Failed)
                    }
                });
            }

            return Json(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template analytics");
            return Json(new { success = false, message = "Template analytics yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Template dönüşüm verilerini getirir (sipariş/ciro bilgisi)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTemplateConversions(string templateName, DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var user = await GetCurrentUserWithCompanyAsync();
            if (user?.Company == null)
            {
                return Json(new { success = false, message = "Şirket bilgisi bulunamadı." });
            }

            var result = await _conversionService.GetConversionsAsync(
                user.Company.Id,
                templateName,
                startDate,
                endDate);

            if (result.Success)
            {
                return Json(new
                {
                    success = true,
                    orderCount = result.OrderCount,
                    totalRevenue = result.TotalRevenue,
                    currency = result.Currency
                });
            }

            return Json(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving template conversions for {TemplateName}", templateName);
            return Json(new { success = false, message = "Dönüşüm verileri yüklenirken bir hata oluştu." });
        }
    }
     
    /// <summary>
    /// Mevcut şirketin WhatsApp bağlantı ayarlarını getirir
    /// </summary>
    private async Task<WhatsAppConnectionSettings?> GetWhatsAppSettingsAsync()
    {
        try
        {
            var companyId = GetCurrentUserCompanyId();
            if (companyId == null) return null;

            var whatsappIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId.Value &&
                                           ci.Integration.Name == "WhatsApp" &&
                                           ci.IsActive &&
                                           ci.IsConfigured);

            if (whatsappIntegration?.Settings == null)
                return null;

            var settings = whatsappIntegration.Settings;
            return new WhatsAppConnectionSettings
            {
                AppId = settings.GetValueOrDefault("appId")?.ToString() ?? "",
                Token = settings.GetValueOrDefault("token")?.ToString() ?? "",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WhatsApp settings for company");
            return null;
        }
    }

    /// <summary>
    /// Appends tracking GUID parameter to a URL for conversion tracking
    /// Handles URLs with existing query strings
    /// </summary>
    private string AppendTrackingParameter(string url, Guid trackingGuid)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        var trackingParam = $"push={trackingGuid}";
        
        // Check if URL already has query parameters
        if (url.Contains('?'))
        {
            // Append with &
            return url + "&" + trackingParam;
        }
        else
        {
            // Append with ?
            return url + "?" + trackingParam;
        }
    }
}

// Request models
public class CreateWhatsAppTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "MARKETING";
    public string Language { get; set; } = "tr";
    public string? ModuleType { get; set; }  // NEW: Hangi modül için kullanılacak
    /// <summary>
    /// Original media URL used in template header (for IMAGE/VIDEO/DOCUMENT templates).
    /// Stored locally so it can be used when sending template messages.
    /// </summary>
    public string? MediaUrl { get; set; }
    public List<TemplateComponent> Components { get; set; } = new();
    
    [JsonPropertyName("variableMappings")]
    public Dictionary<string, Dictionary<int, VariableMappingInfo>>? VariableMappings { get; set; }
}



public class DeleteWhatsAppTemplateRequest
{
    [JsonPropertyName("templateName")]
    public string TemplateName { get; set; }
}

public class CreateFromSampleRequest
{
    public string SampleId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
}
