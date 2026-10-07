using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.ViewModels;
using System.Security.Claims;
using System.Text.Json;

using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class EmailTemplateController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IActivityLogService _activityLogService;

    public EmailTemplateController(
        ApplicationDbContext context,
        IEmailTemplateService emailTemplateService,
        IActivityLogService activityLogService,
        IUserContextService userContextService)
        : base(userContextService, context)
    {
        _context = context;
        _emailTemplateService = emailTemplateService;
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

        // Check if SMTP is configured
        var hasSmtpConfig = await _emailTemplateService.HasSmtpConfigurationAsync(user.Company.Id);
        if (!hasSmtpConfig)
        {
            TempData["ErrorMessage"] = "Mail şablonlarını kullanabilmek için önce SMTP entegrasyonunu yapılandırmanız gerekiyor.";
            return RedirectToAction("Index", "Integration");
        }

        // Get user templates
        var userTemplates = await _emailTemplateService.GetUserTemplatesAsync(user.Company.Id, user.Id);
        
        // Get system templates
        var systemTemplates = await _emailTemplateService.GetSystemTemplatesAsync(user.Company.Id);

        // Keep existing for compatibility until View is fully updated
        var groupedTemplates = await _emailTemplateService.GetGroupedTemplatesAsync(user.Company.Id, user.Id);

        var viewModel = new EmailTemplateIndexViewModel
        {
            UserTemplates = userTemplates,
            SystemTemplates = systemTemplates,
            GroupedTemplates = groupedTemplates, // For backward compatibility
            CompanyId = user.Company.Id
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplate(int templateId, int? companyTemplateId = null)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var userId = user.Id;

        // If companyTemplateId is provided, this is a custom template
        if (companyTemplateId.HasValue)
        {
            var customTemplate = await _context.CompanyEmailTemplates
                .Include(cet => cet.LastModifiedByUser)
                .FirstOrDefaultAsync(cet => cet.Id == companyTemplateId.Value && cet.CompanyId == user.Company.Id);

            if (customTemplate == null)
            {
                return Json(ApiResponse.Error("Özel şablon bulunamadı."));
            }

            // Read metadata from columns
            var customResponse = new
            {
                success = true,
                template = new
                {
                    id = 1000 + customTemplate.Id, // Use same ID format as service
                    companyTemplateId = customTemplate.Id,
                    name = customTemplate.Name ?? "Özel Şablon",
                    category = customTemplate.Category ?? "Özel",
                    description = customTemplate.Description ?? "",
                    moduleType = customTemplate.ModuleType ?? "",
                    defaultSubject = customTemplate.CustomSubject,
                    defaultContent = customTemplate.CustomContent,
                    customSubject = customTemplate.CustomSubject,
                    customContent = customTemplate.CustomContent,
                    variables = TemplateVariableHelper.GetVariablesForModule(customTemplate.ModuleType ?? ""),
                    isCustomized = true,
                    isCustomTemplate = true,
                    lastModified = customTemplate.UpdatedAt,
                    lastModifiedBy = customTemplate.LastModifiedByUser?.FullName
                }
            };

            return Json(customResponse);
        }

        // Regular base template
        var baseTemplate = await _context.EmailTemplates.FindAsync(templateId);
        if (baseTemplate == null)
        {
            return Json(ApiResponse.Error("Şablon bulunamadı."));
        }

        // Get company customization if exists
        var companyTemplate = await _emailTemplateService.GetCompanyTemplateAsync(user.Company.Id, templateId);

        var response = new
        {
            success = true,
            template = new
            {
                id = baseTemplate.Id,
                companyTemplateId = companyTemplate?.Id,
                name = baseTemplate.Name,
                category = baseTemplate.Category,
                description = baseTemplate.Description,
                defaultSubject = baseTemplate.DefaultSubject,
                defaultContent = baseTemplate.DefaultContent,
                customSubject = companyTemplate?.CustomSubject,
                customContent = companyTemplate?.CustomContent,
                variables = baseTemplate.Variables != null ? JsonSerializer.Deserialize<string[]>(baseTemplate.Variables) : new string[0],
                isCustomized = companyTemplate != null,
                isCustomTemplate = false,
                lastModified = companyTemplate?.UpdatedAt,
                lastModifiedBy = companyTemplate?.LastModifiedByUser?.FullName
            }
        };

        return Json(response);
    }

    [HttpGet]
    public IActionResult GetVariables(string moduleType)
    {
        var variables = TemplateVariableHelper.GetVariablesForModule(moduleType);
        return Json(new { success = true, variables = variables.Select(v => new { name = v.Key, description = v.Value }) });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> SaveTemplate([FromBody] SaveTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var userId = user.Id;

        bool success;

        // If companyTemplateId is provided, update the custom template directly
        if (request.CompanyTemplateId.HasValue)
        {
            var customTemplate = await _context.CompanyEmailTemplates
                .FirstOrDefaultAsync(cet => cet.Id == request.CompanyTemplateId.Value && cet.CompanyId == user.Company.Id);

            if (customTemplate == null)
            {
                return Json(ApiResponse.Error("Özel şablon bulunamadı."));
            }

            // Update template directly using columns
            customTemplate.CustomContent = request.Content;
            customTemplate.CustomSubject = request.Subject;
            customTemplate.LastModifiedBy = userId;
            customTemplate.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            success = true;
        }
        else
        {
            // Regular base template customization
            success = await _emailTemplateService.SaveCompanyTemplateAsync(
                user.Company.Id,
                request.TemplateId,
                request.Content,
                request.Subject,
                userId);
        }

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                user.Company.Id,
                ActivityActionTypes.Update,
                ActivityModules.EmailTemplate,
                $"E-posta şablonu güncellendi (ID: {request.TemplateId})",
                entityId: request.CompanyTemplateId?.ToString() ?? request.TemplateId.ToString(),
                entityType: "EmailTemplate",
                details: new { request.TemplateId, request.CompanyTemplateId, SubjectLength = request.Subject?.Length ?? 0 },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SuccessMessage("Şablon başarıyla kaydedildi."));
        }
        else
        {
            return Json(ApiResponse.Error("Şablon kaydedilirken hata oluştu."));
        }
    }

    [HttpPost]
    public async Task<IActionResult> PreviewTemplate([FromBody] PreviewTemplateRequest request)
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
            ["basketUrl"] = "#",
            ["orderNumber"] = "ORD-2024-001",
            ["orderDate"] = DateTime.Now.ToString("dd.MM.yyyy"),
            ["orderTotal"] = "₺299,90",
            ["deliveryAddress"] = "Kadıköy, İstanbul",
            ["trackingUrl"] = "#"
        };

        string content;

        // If this is a custom template or user provided content, use that
        if (!string.IsNullOrEmpty(request.Content))
        {
            content = request.Content;
        }
        else if (request.CompanyTemplateId.HasValue)
        {
            // Get custom template content
            var customTemplate = await _context.CompanyEmailTemplates
                .FirstOrDefaultAsync(cet => cet.Id == request.CompanyTemplateId.Value && cet.CompanyId == user.Company.Id);

            if (customTemplate == null)
            {
                return Json(ApiResponse.Error("Özel şablon bulunamadı."));
            }

            content = customTemplate.CustomContent;
        }
        else
        {
            // Get base template content
            var baseTemplate = await _context.EmailTemplates.FindAsync(request.TemplateId);
            if (baseTemplate == null)
            {
                return Json(ApiResponse.Error("Şablon bulunamadı."));
            }

            // Check if company has customization
            var companyTemplate = await _emailTemplateService.GetCompanyTemplateAsync(user.Company.Id, request.TemplateId);
            content = companyTemplate?.CustomContent ?? baseTemplate.DefaultContent;
        }

        // Replace variables with sample data
        foreach (var kvp in sampleData)
        {
            content = content.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }

        return Json(new { success = true, preview = content });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateEmailTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var userId = user.Id;

        await _emailTemplateService.CreateCustomTemplateAsync(
            user.Company.Id,
            request.Name,
            request.Category,
            request.Description,
            request.Subject,
            request.Content,
            userId,
            request.ModuleType);

        await _activityLogService.LogActivityAsync(
            userId,
            user.Company.Id,
            ActivityActionTypes.Create,
            ActivityModules.EmailTemplate,
            $"Yeni e-posta şablonu oluşturuldu: {request.Name}",
            entityType: "EmailTemplate",
            details: new { request.Name, request.Category },
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(ApiResponse.SuccessMessage("Şablon başarıyla oluşturuldu."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetTemplate([FromBody] ResetTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        var companyTemplate = await _context.CompanyEmailTemplates
            .FirstOrDefaultAsync(cet => cet.CompanyId == user.Company.Id && cet.EmailTemplateId == request.TemplateId);

        if (companyTemplate != null)
        {
            _context.CompanyEmailTemplates.Remove(companyTemplate);
            await _context.SaveChangesAsync();

            await _activityLogService.LogActivityAsync(
                user.Id,
                user.Company.Id,
                ActivityActionTypes.Update,
                ActivityModules.EmailTemplate,
                $"E-posta şablonu varsayılana sıfırlandı (ID: {request.TemplateId})",
                entityId: request.TemplateId.ToString(),
                entityType: "EmailTemplate",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(ApiResponse.SuccessMessage("Şablon varsayılan haline sıfırlandı."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> DeleteTemplate([FromBody] DeleteEmailTemplateRequest request)
    {
        var user = await GetCurrentUserWithCompanyAsync();

        if (user?.Company == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        // Find the custom template
        var companyTemplate = await _context.CompanyEmailTemplates
            .FirstOrDefaultAsync(cet => cet.Id == request.CompanyTemplateId && cet.CompanyId == user.Company.Id && cet.EmailTemplateId == null);

        if (companyTemplate == null)
        {
            return Json(ApiResponse.Error("Şablon bulunamadı veya silinemez."));
        }

        // Get template name for logging
        var name = companyTemplate.Name ?? "Özel Şablon";

        // Delete the template
        _context.CompanyEmailTemplates.Remove(companyTemplate);
        await _context.SaveChangesAsync();

        await _activityLogService.LogActivityAsync(
            user.Id,
            user.Company.Id,
            ActivityActionTypes.Delete,
            ActivityModules.EmailTemplate,
            $"Özel e-posta şablonu silindi: {name}",
            entityId: request.CompanyTemplateId.ToString(),
            entityType: "EmailTemplate",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(ApiResponse.SuccessMessage("Şablon başarıyla silindi."));
    }
}
