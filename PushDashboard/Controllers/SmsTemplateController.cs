using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.ViewModels;
using System.Text.Json;

using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class SmsTemplateController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsTemplateService _smsTemplateService;
    private readonly IActivityLogService _activityLogService;

    public SmsTemplateController(
        ApplicationDbContext context,
        ISmsTemplateService smsTemplateService,
        IActivityLogService activityLogService,
        IUserContextService userContextService)
        : base(userContextService, context)
    {
        _context = context;
        _smsTemplateService = smsTemplateService;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction("Index", "Home");
        }

        // Check if NetGSM is configured
        var hasSmsIntegration = await _smsTemplateService.HasSmsIntegrationAsync(user.Company.Id);
        if (!hasSmsIntegration)
        {
            TempData["ErrorMessage"] = "SMS şablonlarını kullanabilmek için önce NetGSM entegrasyonunu yapılandırmanız gerekiyor.";
            return RedirectToAction("Index", "Integration");
        }

        // Get company's SMS templates directly
        var companyTemplates = await _context.CompanySmsTemplates
            .Include(cst => cst.SmsTemplate)
            .Where(cst => cst.CompanyId == user.Company.Id && cst.IsEnabled)
            .OrderBy(cst => cst.CreatedAt)
            .ToListAsync();

        // One-time self-healing migration for existing data
        bool migrationNeeded = false;
        foreach (var t in companyTemplates)
        {
            if (string.IsNullOrEmpty(t.Name)) // Logic to detect un-migrated data
            {
                if (t.SmsTemplateId.HasValue && t.SmsTemplate != null)
                {
                    // Copy from system template
                    t.Name = t.SmsTemplate.Name;
                    t.Category = t.SmsTemplate.Category;
                    t.Description = t.SmsTemplate.Description;
                    // Try to resolve module type
                    if (t.SmsTemplate.ModuleId.HasValue)
                    {
                        var module = await _context.Modules.FindAsync(t.SmsTemplate.ModuleId.Value);
                        if (module != null) t.ModuleType = _smsTemplateService.GetModuleKeyByName(module.Name);
                    }
                }
                else
                {
                    // Parse from CustomContent
                    var meta = ParseCustomTemplateMetadata(t.CustomContent);
                    t.Name = meta.name;
                    t.Category = meta.category;
                    t.Description = meta.description;
                    if (meta.moduleId.HasValue)
                    {
                        var module = await _context.Modules.FindAsync(meta.moduleId.Value);
                        if (module != null) t.ModuleType = _smsTemplateService.GetModuleKeyByName(module.Name);
                    }
                    t.CustomContent = meta.cleanContent; // Update content to be clean
                }
                migrationNeeded = true;
            }
        }

        if (migrationNeeded)
        {
            await _context.SaveChangesAsync();
        }

        // Get available modules for selection
        var availableModules = await _context.Modules
            .Include(m => m.Category)
            .Where(m => m.IsActive)
            .OrderBy(m => m.Category.SortOrder)
            .ThenBy(m => m.Name)
            .ToListAsync();

        var viewModel = new SmsTemplateIndexViewModel
        {
            CompanyTemplates = companyTemplates,
            CompanyId = user.Company.Id,
            AvailableModules = availableModules
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplate(int? templateId = null ,int? companyTemplateId = null)
    {
        var user = await GetCurrentUserWithCompanyAsync();
        if (user?.Company == null) return Json(ApiResponse.CompanyNotFound);
        
        object response = null;
        if (companyTemplateId.HasValue)
        {
            var companyTemplate = await _context.CompanySmsTemplates
                .Include(cst => cst.SmsTemplate)
                .Include(cst => cst.LastModifiedByUser)
                .FirstOrDefaultAsync(cst => cst.Id == companyTemplateId.Value && cst.CompanyId == user.Company.Id);

            if (companyTemplate == null)
            {
                return Json(ApiResponse.Error("Şablon bulunamadı."));
            }

            // Return properties directly from CompanySmsTemplate (now fully decoupled)
            response = new
            {
                Id = companyTemplate.Id,
                Name = companyTemplate.Name,
                Category = companyTemplate.Category,
                Description = companyTemplate.Description,
                Content = companyTemplate.CustomContent,
                IsCustomized = true, 
                IsCustomTemplate = !companyTemplate.SmsTemplateId.HasValue,
                ModuleId = companyTemplate.SmsTemplateId, // Just for reference if needed
                ModuleType = companyTemplate.ModuleType
            };
        }
        else if (templateId.HasValue)
        {
            var smsTemplate = await _context.SmsTemplates
                .FirstOrDefaultAsync(cst => cst.Id == templateId.Value);

            if (smsTemplate == null)
            {
                return Json(ApiResponse.Error("Şablon bulunamadı."));
            }

            // Return properties directly from CompanySmsTemplate (now fully decoupled)
            response = new
            {
                Id = smsTemplate.Id,
                Name = smsTemplate.Name,
                Category = smsTemplate.Category,
                Description = smsTemplate.Description,
                Content = smsTemplate.DefaultContent,
                IsCustomized = true, 
                IsCustomTemplate = false,
                ModuleId = smsTemplate.ModuleId, // Just for reference if needed
                ModuleType = await _smsTemplateService.ResolveModuleTypeAsync(smsTemplate.ModuleId)
            };
        }
        else
            return Json(ApiResponse.Error("Şablon ID'si bulunamadı."));
        

        return Json(new { success = true, template = response });
    }

    private (string name, string category, string description, int? moduleId, string cleanContent) ParseCustomTemplateMetadata(string content)
    {
        var name = "Özel Şablon";
        var category = "Özel";
        var description = "";
        int? moduleId = null;
        var cleanContent = content;

        // Look for metadata comment at the beginning
        if (!string.IsNullOrEmpty(content) && content.StartsWith("<!-- CUSTOM_TEMPLATE:"))
        {
            var endIndex = content.IndexOf("-->");
            if (endIndex > 0)
            {
                var metadataLine = content.Substring(0, endIndex + 3);
                cleanContent = content.Substring(endIndex + 3).TrimStart('\n', '\r');

                // Parse metadata: <!-- CUSTOM_TEMPLATE: name | category | description | moduleId -->
                var metadataContent = metadataLine.Replace("<!-- CUSTOM_TEMPLATE:", "").Replace("-->", "").Trim();
                var parts = metadataContent.Split('|');

                if (parts.Length >= 1) name = parts[0].Trim();
                if (parts.Length >= 2) category = parts[1].Trim();
                if (parts.Length >= 3) description = parts[2].Trim();
                if (parts.Length >= 4 && int.TryParse(parts[3].Trim(), out int parsedModuleId)) moduleId = parsedModuleId;
            }
        }

        return (name, category, description, moduleId, cleanContent);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> SaveTemplate([FromBody] SaveSmsTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var userId = user.Id;

        // Now we always work with companyTemplateId
        if (!request.CompanyTemplateId.HasValue)
        {
            return Json(ApiResponse.Error("Şablon ID'si bulunamadı."));
        }

        var companyTemplate = await _context.CompanySmsTemplates
            .FirstOrDefaultAsync(cst => cst.Id == request.CompanyTemplateId.Value && cst.CompanyId == user.Company.Id);

        if (companyTemplate == null)
        {
            return Json(ApiResponse.Error("Şablon bulunamadı."));
        }

        // Update content directly - clean, no metadata
        companyTemplate.CustomContent = request.Content;
        companyTemplate.LastModifiedBy = userId;
        companyTemplate.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        
        await _activityLogService.LogActivityAsync(
            userId,
            user.Company.Id,
            ActivityActionTypes.Update,
            ActivityModules.SmsTemplate,
            $"SMS şablonu güncellendi (ID: {request.TemplateId})",
            entityId: request.CompanyTemplateId?.ToString() ?? request.TemplateId.ToString(),
            entityType: "SmsTemplate",
            details: new { request.TemplateId, request.CompanyTemplateId, ContentLength = request.Content?.Length ?? 0 },
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(ApiResponse.SuccessMessage("Şablon başarıyla kaydedildi."));
    }

    [HttpPost]
    public async Task<IActionResult> PreviewTemplate([FromBody] PreviewSmsTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        // Sample data for preview
        var sampleData = new Dictionary<string, string>
        {
            ["customerName"] = "Ahmet Yılmaz",
            ["customerEmail"] = "ahmet@example.com",
            ["companyName"] = "Örnek Şirket",
            ["companyAddress"] = "İstanbul, Türkiye",
            ["registrationDate"] = DateTime.Now.ToString("dd.MM.yyyy"),
            ["updateDate"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
            ["updatedFields"] = "Ad, Soyad, Telefon",
            ["itemCount"] = "3",
            ["basketTotal"] = "₺299,90",
            ["basketUrl"] = "example.com/sepet",
            ["orderNumber"] = "ORD-2024-001",
            ["orderDate"] = DateTime.Now.ToString("dd.MM.yyyy"),
            ["orderTotal"] = "₺299,90",
            ["deliveryAddress"] = "Kadıköy, İstanbul",
            ["trackingUrl"] = "example.com/takip"
        };

        string content;

        // If user provided content, use that
        if (!string.IsNullOrEmpty(request.Content))
        {
            content = request.Content;
        }
        else if (request.CompanyTemplateId.HasValue)
        {
            // Get custom template content
            var customTemplate = await _context.CompanySmsTemplates
                .FirstOrDefaultAsync(cst => cst.Id == request.CompanyTemplateId.Value && cst.CompanyId == user.Company.Id);

            if (customTemplate == null)
            {
                return Json(ApiResponse.Error("Özel şablon bulunamadı."));
            }

            // Parse metadata and get clean content
            var (_, _, _, _, cleanContent) = ParseCustomTemplateMetadata(customTemplate.CustomContent);
            content = cleanContent;
        }
        else
        {
            // Get base template content
            var baseTemplate = await _context.SmsTemplates.FindAsync(request.TemplateId);
            if (baseTemplate == null)
            {
                return Json(ApiResponse.Error("Şablon bulunamadı."));
            }

            // Check if company has customization
            var companyTemplate = await _smsTemplateService.GetCompanyTemplateAsync(user.Company.Id, request.TemplateId);
            content = companyTemplate?.CustomContent ?? baseTemplate.DefaultContent;
        }

        // Replace variables with sample data
        var preview = await _smsTemplateService.ReplaceVariablesAsync(content, sampleData);

        return Json(new { success = true, preview = preview, characterCount = preview.Length });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateSmsTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var userId = user.Id;

        await _smsTemplateService.CreateCustomTemplateAsync(
            user.Company.Id,
            request.Name,
            request.Category,
            request.Description,
            request.Content,
            userId,
            request.ModuleType);

        await _activityLogService.LogActivityAsync(
            userId,
            user.Company.Id,
            ActivityActionTypes.Create,
            ActivityModules.SmsTemplate,
            $"Yeni SMS şablonu oluşturuldu: {request.Name}",
            entityType: "SmsTemplate",
            details: new { request.Name, request.Category },
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(ApiResponse.SuccessMessage("Şablon başarıyla oluşturuldu."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetTemplate([FromBody] ResetSmsTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var companyTemplate = await _context.CompanySmsTemplates
            .FirstOrDefaultAsync(cst => cst.CompanyId == user.Company.Id && cst.SmsTemplateId == request.TemplateId);

        if (companyTemplate != null)
        {
            _context.CompanySmsTemplates.Remove(companyTemplate);
            await _context.SaveChangesAsync();

            await _activityLogService.LogActivityAsync(
                user.Id,
                user.Company.Id,
                ActivityActionTypes.Update,
                ActivityModules.SmsTemplate,
                $"SMS şablonu varsayılana sıfırlandı (ID: {request.TemplateId})",
                entityId: request.TemplateId.ToString(),
                entityType: "SmsTemplate",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(ApiResponse.SuccessMessage("Şablon varsayılan haline sıfırlandı."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> DeleteTemplate([FromBody] DeleteSmsTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        // Find the custom template (only custom templates can be deleted, SmsTemplateId should be null)
        var companyTemplate = await _context.CompanySmsTemplates
            .FirstOrDefaultAsync(cst => cst.Id == request.CompanyTemplateId && cst.CompanyId == user.Company.Id && cst.SmsTemplateId == null);

        if (companyTemplate == null)
        {
            return Json(ApiResponse.Error("Şablon bulunamadı veya silinemez."));
        }

        // Parse template name for logging
        var (name, _, _, _, _) = ParseCustomTemplateMetadata(companyTemplate.CustomContent);

        // Delete the template
        _context.CompanySmsTemplates.Remove(companyTemplate);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            user.Id,
            user.Company.Id,
            ActivityActionTypes.Delete,
            ActivityModules.SmsTemplate,
            $"Özel SMS şablonu silindi: {name}",
            entityId: request.CompanyTemplateId.ToString(),
            entityType: "SmsTemplate",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(ApiResponse.SuccessMessage("Şablon başarıyla silindi."));
    }
    
    [HttpGet]
    public async Task<IActionResult> GetSystemTemplates()
    {
        var user = await GetCurrentUserWithCompanyAsync();
        if (user?.Company == null) return Json(ApiResponse.CompanyNotFound);

        // Fetch system templates (ready-to-use)
        var templates = await _smsTemplateService.GetSystemTemplatesAsync(user.Company.Id);
        
        var existingSystemTemplateIds = await _context.CompanySmsTemplates
            .Where(cst => cst.CompanyId == user.Company.Id && cst.SmsTemplateId != null)
            .Select(cst => cst.SmsTemplateId.Value)
            .ToListAsync();

        // Helper to get module name efficiently
        var modules = await _context.Modules.ToDictionaryAsync(m => m.Id, m => m.Name);

        var result = templates.Select(t => new 
        {
            id = t.Id,
            name = t.Name,
            category = t.Category,
            description = t.Description,
            content = t.DefaultContent,
            moduleId = t.ModuleId,
            moduleName = t.ModuleId.HasValue && modules.ContainsKey(t.ModuleId.Value) ? modules[t.ModuleId.Value] : null,
            isAdded = existingSystemTemplateIds.Contains(t.Id)
        }).ToList();

        return Json(new { success = true, templates = result });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFromSystemTemplate([FromBody] AddSystemTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();
        if (user?.Company == null) return Json(ApiResponse.CompanyNotFound);

        var success = await _smsTemplateService.CopySystemTemplateToCompanyAsync(user.Company.Id, request.TemplateId, user.Id);

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.Company.Id,
                ActivityActionTypes.Create,
                ActivityModules.SmsTemplate,
                $"Hazır şablon eklendi (ID: {request.TemplateId})",
                entityId: request.TemplateId.ToString(),
                entityType: "SmsTemplate",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SuccessMessage("Şablon listenize eklendi."));
        }
        else
        {
            return Json(ApiResponse.Error("Şablon eklenirken hata oluştu veya bu şablon zaten listenizde var."));
        }
    }


    [HttpGet]
    public IActionResult GetModuleVariables(string moduleType)
    {
        var variables = TemplateVariableHelper.GetVariablesForModule(moduleType);
        return Json(new { success = true, variables = variables.Select(k => new { key = k.Key, label = k.Value }) });
    }


    public class AddSystemTemplateRequest
    {
        public int TemplateId { get; set; }
    }
}

