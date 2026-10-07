using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Services;

public interface IEmailTemplateService
{
    Task<bool> HasSmtpConfigurationAsync(Guid companyId);
    Task<List<EmailTemplate>> GetAvailableTemplatesAsync(Guid companyId);
    Task<CompanyEmailTemplate?> GetCompanyTemplateAsync(Guid companyId, int templateId);
    Task<bool> SaveCompanyTemplateAsync(Guid companyId, int templateId, string content, string subject, string userId);
    Task<string> PreviewTemplateAsync(int templateId, Dictionary<string, string> sampleData);
    Task<Dictionary<string, List<EmailTemplate>>> GetGroupedTemplatesAsync(Guid companyId, string userId); // Deprecated but kept for compatibility or refactor
    Task<Dictionary<string, List<EmailTemplate>>> GetSystemTemplatesAsync(Guid companyId);
    Task<Dictionary<string, List<CompanyEmailTemplate>>> GetUserTemplatesAsync(Guid companyId, string userId);
    Task<List<CompanyEmailTemplate>> GetCompanyTemplatesAsync(Guid companyId);
    Task CreateCustomTemplateAsync(Guid companyId, string name, string category, string description, string subject, string content, string userId, string? moduleType = null);
}

public class EmailTemplateService : IEmailTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(ApplicationDbContext context, ILogger<EmailTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasSmtpConfigurationAsync(Guid companyId)
    {
        try
        {
            // Check if company has SMTP integration configured and active
            var smtpIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "Email SMTP" &&
                                         ci.IsActive);

            if (smtpIntegration == null)
                return false;

            // Validate required SMTP settings
            var requiredFields = new[] { "smtpServer", "username", "password", "fromEmail" };
            foreach (var field in requiredFields)
            {
                if (!smtpIntegration.Settings.ContainsKey(field) || string.IsNullOrWhiteSpace(smtpIntegration.Settings[field]?.ToString()))
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SMTP configuration for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<List<EmailTemplate>> GetAvailableTemplatesAsync(Guid companyId)
    {
        try
        {
            // Get custom templates for this company
            var customTemplateData = await _context.CompanyEmailTemplates
                .Where(cet => cet.CompanyId == companyId && cet.EmailTemplateId == null && cet.IsEnabled)
                .ToListAsync();

            var customTemplates = customTemplateData.Select(cet =>
            {
                // Read metadata from columns (fall back to parsing if columns empty for backward compatibility)
                string templateName, templateCategory, templateDescription, cleanContent;
                
                if (!string.IsNullOrEmpty(cet.Name))
                {
                    // New format: read from columns
                    templateName = cet.Name;
                    templateCategory = cet.Category ?? "Özel";
                    templateDescription = cet.Description ?? "";
                    cleanContent = cet.CustomContent;
                }
                else
                {
                    // Backward compatibility: parse from HTML comment
                    templateName = cet.CustomSubject ?? "Özel Şablon";
                    templateCategory = "Özel";
                    templateDescription = cet.CustomSubject ?? "";
                    cleanContent = cet.CustomContent;
                }

                return new EmailTemplate
                {
                    Id = 1000 + cet.Id, // Use a high number + CompanyEmailTemplate.Id to ensure uniqueness
                    Name = templateName,
                    Category = templateCategory,
                    Description = templateDescription,
                    DefaultContent = cleanContent,
                    DefaultSubject = cet.CustomSubject,
                    IsActive = true,
                    SortOrder = 1000, // Put custom templates at the end
                    CreatedAt = cet.CreatedAt,
                    UpdatedAt = cet.UpdatedAt,
                    // Store CompanyEmailTemplate.Id in ModuleId field temporarily for frontend
                    ModuleId = cet.Id
                };
            }).ToList();

            // Combine base templates and custom templates
            return customTemplates.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available templates for company {CompanyId}", companyId);
            return new List<EmailTemplate>();
        }
    }

    public async Task<CompanyEmailTemplate?> GetCompanyTemplateAsync(Guid companyId, int templateId)
    {
        try
        {
            // For custom templates, templateId is positive but stored as negative
            var searchTemplateId = templateId;

            // First try to find as regular template
            var template = await _context.CompanyEmailTemplates
                .Include(cet => cet.EmailTemplate)
                .Include(cet => cet.LastModifiedByUser)
                .FirstOrDefaultAsync(cet => cet.CompanyId == companyId && cet.EmailTemplateId == templateId);

            // If not found and templateId is positive, try as custom template (negative ID)
            if (template == null)
            {
                searchTemplateId = -templateId;
                template = await _context.CompanyEmailTemplates
                    .Include(cet => cet.EmailTemplate)
                    .Include(cet => cet.LastModifiedByUser)
                    .FirstOrDefaultAsync(cet => cet.CompanyId == companyId && cet.EmailTemplateId == searchTemplateId);
            }

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company template for company {CompanyId}, template {TemplateId}", companyId, templateId);
            return null;
        }
    }

    public async Task<bool> SaveCompanyTemplateAsync(Guid companyId, int templateId, string content, string subject, string userId)
    {
        try
        {
            // Try to find existing template (both positive and negative IDs)
            var existingTemplate = await _context.CompanyEmailTemplates
                .FirstOrDefaultAsync(cet => cet.CompanyId == companyId &&
                    (cet.EmailTemplateId == templateId || cet.EmailTemplateId == -templateId));

            if (existingTemplate != null)
            {
                // Update existing template
                existingTemplate.CustomContent = content;
                existingTemplate.CustomSubject = subject;
                existingTemplate.LastModifiedBy = userId;
                existingTemplate.UpdatedAt = DateTime.Now;
            }
            else
            {
                // Create new company template
                var newTemplate = new CompanyEmailTemplate
                {
                    CompanyId = companyId,
                    EmailTemplateId = templateId,
                    CustomContent = content,
                    CustomSubject = subject,
                    LastModifiedBy = userId,
                    IsEnabled = true
                };

                await _context.CompanyEmailTemplates.AddAsync(newTemplate);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving company template for company {CompanyId}, template {TemplateId}", companyId, templateId);
            return false;
        }
    }

    public async Task<string> PreviewTemplateAsync(int templateId, Dictionary<string, string> sampleData)
    {
        try
        {
            var template = await _context.EmailTemplates.FindAsync(templateId);
            if (template == null)
                return "Template not found";

            var content = template.DefaultContent;

            // Replace variables with sample data
            foreach (var kvp in sampleData)
            {
                content = content.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing template {TemplateId}", templateId);
            return "Error generating preview";
        }
    }

    public async Task<Dictionary<string, List<EmailTemplate>>> GetGroupedTemplatesAsync(Guid companyId, string userId)
    {
        try
        {
            var templates = await GetAvailableTemplatesAsync(companyId);

            return templates
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grouped templates for company {CompanyId}", companyId);
            return new Dictionary<string, List<EmailTemplate>>();
        }
    }

    public async Task CreateCustomTemplateAsync(Guid companyId, string name, string category, string description, string subject, string content, string userId, string? moduleType = null)
    {
        try
        {
            // Store template metadata directly in columns instead of HTML comment
            var companyTemplate = new CompanyEmailTemplate
            {
                CompanyId = companyId,
                EmailTemplateId = null, // Null for custom templates
                Name = name,
                Category = category,
                Description = description,
                CustomContent = content, // Clean content without metadata comment
                CustomSubject = subject,
                ModuleType = moduleType,
                LastModifiedBy = userId,
                IsEnabled = true,
            };

            await _context.CompanyEmailTemplates.AddAsync(companyTemplate);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom template for company {CompanyId}", companyId);
        }
    }
    public async Task<Dictionary<string, List<EmailTemplate>>> GetSystemTemplatesAsync(Guid companyId)
    {
        try
        {
            // Get company's purchased modules
            var companyModuleIds = await _context.CompanyModules
                .Where(cm => cm.CompanyId == companyId && cm.IsActive)
                .Select(cm => cm.ModuleId)
                .ToListAsync();

            // Get base templates that are available (System templates)
            // System templates are those in EmailTemplates table
            var baseTemplates = await _context.EmailTemplates
                .Where(et => et.IsActive && (et.ModuleId == null || companyModuleIds.Contains(et.ModuleId.Value)))
                .OrderBy(et => et.Category)
                .ThenBy(et => et.SortOrder)
                .ToListAsync();

            return baseTemplates
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system templates for company {CompanyId}", companyId);
            return new Dictionary<string, List<EmailTemplate>>();
        }
    }

    public async Task<Dictionary<string, List<CompanyEmailTemplate>>> GetUserTemplatesAsync(Guid companyId, string userId)
    {
        try
        {
            // Get custom templates for this company (CompanyEmailTemplates)
            var customTemplates = await _context.CompanyEmailTemplates
                .Where(cet => cet.CompanyId == companyId && cet.EmailTemplateId == null && cet.IsEnabled)
                .ToListAsync();
            
            return customTemplates
                .GroupBy(t => GetModuleTypeDisplayName(t.ModuleType ?? ""))
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user templates for company {CompanyId}", companyId);
            return new Dictionary<string, List<CompanyEmailTemplate>>();
        }
    }

    public async Task<List<CompanyEmailTemplate>> GetCompanyTemplatesAsync(Guid companyId)
    {
        // Get custom templates for this company (CompanyEmailTemplates)
        return await _context.CompanyEmailTemplates
            .Where(cet => cet.CompanyId == companyId && cet.EmailTemplateId == null && cet.IsEnabled)
            .ToListAsync();
    }


    private string GetModuleTypeDisplayName(string moduleType)
    {
        return moduleType switch
        {
            "OrderStatus" => "Sipariş Durumu Bildirimleri",
            "BulkMessaging" => "Toplu Mesaj Gönderimi",
            "CartReminder" => "Sepet Hatırlatıcıları",
            "FirstOrder" => "İlk Alışveriş Mesajı",
            "" or null => "Genel / Diğer",
            _ => moduleType
        };
    }
}
