using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services.Notifications;
using PushDashboard.Services.SMS;
using PushDashboard.Services.WhatsApp;
using PushDashboard.ViewModels;

namespace PushDashboard.Services;

/// <summary>
/// Tüm modüller için ortak şablon işlemlerini yöneten servis implementasyonu
/// </summary>
public class TemplateModuleService : ITemplateModuleService
{
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IWhatsAppTemplateService _whatsAppTemplateService;
    private readonly ISmsTemplateService _smsTemplateService;
    private readonly ILogger<TemplateModuleService> _logger;

    public TemplateModuleService(
        IEmailTemplateService emailTemplateService,
        IWhatsAppTemplateService whatsAppTemplateService,
        ISmsTemplateService smsTemplateService,
        ILogger<TemplateModuleService> logger)
    {
        _emailTemplateService = emailTemplateService;
        _whatsAppTemplateService = whatsAppTemplateService;
        _smsTemplateService = smsTemplateService;
        _logger = logger;
    }

    public async Task<List<TemplateOptionViewModel>> GetChannelTemplatesAsync(Guid companyId, string channelType, string? moduleType)
    {
        try
        {
            var templates = new List<TemplateOptionViewModel>();
            
            if (channelType.ToLower() == "email")
            {
                // Get user templates (custom templates with ModuleType)
                var userTemplates = await _emailTemplateService.GetCompanyTemplatesAsync(companyId);
                
                var filteredEmailTemplates = string.IsNullOrEmpty(moduleType) 
                    ? userTemplates 
                    : userTemplates.Where(x => x.ModuleType == moduleType).ToList();
                
                templates.AddRange(filteredEmailTemplates.Select(t => new TemplateOptionViewModel
                {
                    Id = t.Id.ToString(),
                    Name = t.Name ?? "Özel Şablon",
                    DisplayName = t.Description ?? t.Name ?? "Özel Şablon",
                    Subject = t.CustomSubject,
                    PreviewContent = TruncateContent(t.CustomContent, 100),
                    Category = t.Category ?? "Genel"
                }));
            }
            else if (channelType.ToLower() == "sms")
            {
                var smsTemplates = await _smsTemplateService.GetUserTemplatesAsync(companyId);
                
                // ModuleType'a göre filtrele (eğer belirtilmişse)
                var filteredSmsTemplates = string.IsNullOrEmpty(moduleType) 
                    ? smsTemplates 
                    : smsTemplates.Where(x => x.ModuleType == moduleType).ToList();
                    
                templates.AddRange(filteredSmsTemplates.Select(t => new TemplateOptionViewModel
                {
                    Id = t.Id.ToString(),
                    Name = t.Name,
                    DisplayName = t.Description ?? t.Name,
                    Subject = null,
                    PreviewContent = TruncateContent(t.CustomContent, 100),
                    Category = t.Category ?? "Genel"
                }));
            }
            else if (channelType.ToLower() == "whatsapp")
            {
                var whatsAppTemplates = await _whatsAppTemplateService.GetFacebookTemplatesAsync(companyId);
                var templateSettings = await _whatsAppTemplateService.GetTemplateSettingsAsync(companyId);

                // Filter by ModuleType if provided
                if (!string.IsNullOrEmpty(moduleType))
                {
                    // Get template names that match the module type
                    var allowedTemplateNames = templateSettings
                        .Where(x => x.ModuleType == moduleType)
                        .Select(x => x.TemplateName)
                        .ToHashSet();

                    whatsAppTemplates = whatsAppTemplates
                        .Where(x => allowedTemplateNames.Contains(x.Name))
                        .ToList();
                }

                templates.AddRange(whatsAppTemplates.Where(x => x.Status == "APPROVED").Select(t => new TemplateOptionViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    DisplayName = t.Name,
                    Subject = null,
                    PreviewContent = TruncateContent(GetWhatsAppTemplateContent(t), 100),
                    Category = t.Category ?? "Genel"
                }));
            }

            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates for channel {ChannelType} and company {CompanyId}", channelType, companyId);
            return new List<TemplateOptionViewModel>();
        }
    }

    private string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        if (content.Length <= maxLength) return content;
        return content.Substring(0, maxLength) + "...";
    }

    private string GetWhatsAppTemplateContent(FacebookMessageTemplate template)
    {
        // WhatsApp template'inden BODY component'ini al
        try
        {
            if (template.Components != null)
            {
                var bodyComponent = template.Components.FirstOrDefault(c => c.Type == "BODY");
                if (bodyComponent != null && !string.IsNullOrEmpty(bodyComponent.Text))
                {
                    return bodyComponent.Text;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return "WhatsApp şablonu";
    }
}
