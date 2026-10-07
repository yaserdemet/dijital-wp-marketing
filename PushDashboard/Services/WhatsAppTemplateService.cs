using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.WhatsApp;
using System.Text.Json;

namespace PushDashboard.Services;

public interface IWhatsAppTemplateService
{
    Task<bool> HasWhatsAppConfigurationAsync(Guid companyId);
    Task<List<FacebookMessageTemplate>> GetFacebookTemplatesAsync(Guid companyId);
    Task<FacebookMessageTemplate?> GetFacebookTemplateAsync(Guid companyId, string templateId);
    Task<FacebookMessageTemplate?> CreateFacebookTemplateAsync(Guid companyId, CreateTemplateRequest request);

    Task<bool> DeleteFacebookTemplateAsync(Guid companyId, string templateName);
    Task<Dictionary<string, List<FacebookMessageTemplate>>> GetGroupedFacebookTemplatesAsync(Guid companyId);
    
    // Settings Methods
    Task<List<WhatsAppTemplateSettings>> GetTemplateSettingsAsync(Guid companyId);
    
    // Variable Mapping Methods
    Task<List<WhatsAppTemplateVariableMapping>> GetVariableMappingsAsync(Guid companyId, string templateId);
    Task SaveVariableMappingsAsync(Guid companyId, string templateId, Dictionary<string, Dictionary<int, VariableMappingInfo>> mappings);
}

public class WhatsAppTemplateService : IWhatsAppTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<WhatsAppTemplateService> _logger;

    public WhatsAppTemplateService(
        ApplicationDbContext context,
        IWhatsAppService whatsAppService,
        ILogger<WhatsAppTemplateService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<bool> HasWhatsAppConfigurationAsync(Guid companyId)
    {
        try
        {
            var whatsappIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "WhatsApp" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            return whatsappIntegration != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking WhatsApp configuration for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<List<FacebookMessageTemplate>> GetFacebookTemplatesAsync(Guid companyId)
    {
        try
        {
            var settings = await GetWhatsAppSettingsAsync(companyId);
            if (settings == null)
            {
                _logger.LogWarning("WhatsApp settings not found for company {CompanyId}", companyId);
                return new List<FacebookMessageTemplate>();
            }

            var result = await _whatsAppService.GetMessageTemplatesAsync(settings);
            if (result.Success && result.Data != null)
            {
                var templates = result.Data;

                // Enrich templates with local media URLs for preview
                try 
                {
                    var mediaMappings = await _context.WhatsAppTemplateVariableMappings
                        .Where(m => m.CompanyId == companyId && 
                                  m.ComponentType == "HEADER" && 
                                  m.VariableType == "media_url")
                        .ToListAsync();

                    foreach (var template in templates)
                    {
                        var mapping = mediaMappings.FirstOrDefault(m => m.WhatsAppTemplateName == template.Name);
                        if (mapping != null && !string.IsNullOrEmpty(mapping.VariableLabel))
                        {
                            var header = template.Components?.FirstOrDefault(c => c.Type == "HEADER");
                            if (header != null && (header.Format == "IMAGE" || header.Format == "VIDEO" || header.Format == "DOCUMENT"))
                            {
                                // VariableLabel stores the full URL
                                header.Text = mapping.VariableLabel;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enriching templates with media URLs for company {CompanyId}", companyId);
                    // Continue returning templates even if enrichment fails
                }

                return templates;
            }
            else
            {
                _logger.LogError("Failed to get Facebook templates for company {CompanyId}: {Error}", companyId, result.Message);
                return new List<FacebookMessageTemplate>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Facebook templates for company {CompanyId}", companyId);
            return new List<FacebookMessageTemplate>();
        }
    }

    public async Task<FacebookMessageTemplate?> GetFacebookTemplateAsync(Guid companyId, string templateId)
    {
        try
        {
            var settings = await GetWhatsAppSettingsAsync(companyId);
            if (settings == null)
            {
                _logger.LogWarning("WhatsApp settings not found for company {CompanyId}", companyId);
                return null;
            }

            var result = await _whatsAppService.GetMessageTemplateAsync(settings, templateId);
            if (result.Success && result.Data != null)
            {
                var template = result.Data;

                // Enrich template with local media URL for preview
                try
                {
                    var mapping = await _context.WhatsAppTemplateVariableMappings
                        .FirstOrDefaultAsync(m => m.CompanyId == companyId && 
                                                m.WhatsAppTemplateName == template.Name && 
                                                m.ComponentType == "HEADER" && 
                                                m.VariableType == "media_url");

                    if (mapping != null && !string.IsNullOrEmpty(mapping.VariableLabel))
                    {
                        var header = template.Components?.FirstOrDefault(c => c.Type == "HEADER");
                        if (header != null && (header.Format == "IMAGE" || header.Format == "VIDEO" || header.Format == "DOCUMENT"))
                        {
                             // VariableLabel stores the full URL
                             header.Text = mapping.VariableLabel;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enriching template {TemplateId} with media URL for company {CompanyId}", templateId, companyId);
                }

                return template;
            }
            else
            {
                _logger.LogError("Failed to get Facebook template {TemplateId} for company {CompanyId}: {Error}",
                    templateId, companyId, result.Message);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Facebook template {TemplateId} for company {CompanyId}", templateId, companyId);
            return null;
        }
    }

    public async Task<FacebookMessageTemplate?> CreateFacebookTemplateAsync(Guid companyId, CreateTemplateRequest request)
    {
        try
        {
            var settings = await GetWhatsAppSettingsAsync(companyId);
            if (settings == null)
            {
                _logger.LogWarning("WhatsApp settings not found for company {CompanyId}", companyId);
                return null;
            }

            var result = await _whatsAppService.CreateMessageTemplateAsync(settings, request);
            if (result.Success && result.Data != null)
            {
                _logger.LogInformation("Successfully created Facebook template {TemplateName} for company {CompanyId}",
                    request.Name, companyId);
                return result.Data;
            }
            else
            {
                _logger.LogError("Failed to create Facebook template {TemplateName} for company {CompanyId}: {Error}",
                    request.Name, companyId, result.Message);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Facebook template {TemplateName} for company {CompanyId}", request.Name, companyId);
            return null;
        }
    }



    public async Task<bool> DeleteFacebookTemplateAsync(Guid companyId, string templateName)
    {
        try
        {
            var settings = await GetWhatsAppSettingsAsync(companyId);
            if (settings == null)
            {
                _logger.LogWarning("WhatsApp settings not found for company {CompanyId}", companyId);
                return false;
            }

            var result = await _whatsAppService.DeleteMessageTemplateAsync(settings, templateName);
            if (result.Success)
            {
                _logger.LogInformation("Successfully deleted Facebook template {TemplateName} for company {CompanyId}",
                    templateName, companyId);
                return true;
            }
            else
            {
                _logger.LogError("Failed to delete Facebook template {TemplateName} for company {CompanyId}: {Error}",
                    templateName, companyId, result.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Facebook template {TemplateName} for company {CompanyId}", templateName, companyId);
            return false;
        }
    }

    public async Task<Dictionary<string, List<FacebookMessageTemplate>>> GetGroupedFacebookTemplatesAsync(Guid companyId)
    {
        try
        {
            var templates = await GetFacebookTemplatesAsync(companyId);
            return templates.GroupBy(t => t.Category).ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grouped Facebook templates for company {CompanyId}", companyId);
            return new Dictionary<string, List<FacebookMessageTemplate>>();
        }
    }

    public async Task<List<WhatsAppTemplateSettings>> GetTemplateSettingsAsync(Guid companyId)
    {
        try
        {
            return await _context.WhatsAppTemplateSettings
                .Where(x => x.CompanyId == companyId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WhatsApp template settings for company {CompanyId}", companyId);
            return new List<WhatsAppTemplateSettings>();
        }
    }

    private async Task<WhatsAppConnectionSettings?> GetWhatsAppSettingsAsync(Guid companyId)
    {
        try
        {
            var whatsappIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
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
            _logger.LogError(ex, "Error getting WhatsApp settings for company {CompanyId}", companyId);
            return null;
        }
    }
    
    /// <summary>
    /// Template için kaydedilmiş variable mapping'leri getirir
    /// </summary>
    public async Task<List<WhatsAppTemplateVariableMapping>> GetVariableMappingsAsync(Guid companyId, string templateName)
    {
        try
        {
            return await _context.WhatsAppTemplateVariableMappings
                .Where(m => m.CompanyId == companyId && m.WhatsAppTemplateName == templateName)
                .OrderBy(m => m.ComponentType)
                .ThenBy(m => m.VariablePosition)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting variable mappings for template {TemplateName} and company {CompanyId}",
                templateName, companyId);
            return new List<WhatsAppTemplateVariableMapping>();
        }
    }
    
    public async Task SaveVariableMappingsAsync(Guid companyId, string templateName, Dictionary<string, Dictionary<int, VariableMappingInfo>> mappings)
    {
        try
        {
            // Delete existing mappings for this template
            var existingMappings = await _context.WhatsAppTemplateVariableMappings
                .Where(m => m.CompanyId == companyId && m.WhatsAppTemplateName == templateName)
                .ToListAsync();
            
            if (existingMappings.Any())
            {
                _context.WhatsAppTemplateVariableMappings.RemoveRange(existingMappings);
            }
            
            // Add new mappings
            if (mappings != null && mappings.Count > 0)
            {
                foreach (var (componentType, variables) in mappings)
                {
                    foreach (var (position, info) in variables)
                    {
                        _context.WhatsAppTemplateVariableMappings.Add(new WhatsAppTemplateVariableMapping
                        {
                            CompanyId = companyId,
                            WhatsAppTemplateName = templateName,
                            ComponentType = componentType,
                            VariablePosition = position,
                            VariableType = info.VariableType,
                            VariableLabel = info.VariableLabel,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully saved variable mappings for template {TemplateName}", templateName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving variable mappings for template {TemplateName} and company {CompanyId}",
                templateName, companyId);
            throw;
        }
    }
}

// Helper class for variable mapping info
public class VariableMappingInfo
{
    public string VariableType { get; set; } = string.Empty;
    public string VariableLabel { get; set; } = string.Empty;
}
