using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;
using PushDashboard.Services.BulkMessaging;

namespace PushDashboard.Services;

public interface ISmsTemplateService
{
    Task<bool> HasSmsIntegrationAsync(Guid companyId);
    Task<CompanySmsTemplate?> GetCompanyTemplateAsync(Guid companyId, int companyTemplateId);
    Task<bool> SaveCompanyTemplateAsync(Guid companyId, int? companyTemplateId, string content, string userId);
    Task<Dictionary<string, List<CompanySmsTemplate>>> GetGroupedTemplatesAsync(Guid companyId, string userId);
    Task<List<CompanySmsTemplate>> GetUserTemplatesAsync(Guid companyId);
    Task<List<SmsTemplate>> GetSystemTemplatesAsync(Guid companyId);
    Task<bool> CopySystemTemplateToCompanyAsync(Guid companyId, int systemTemplateId, string userId);
    Task CreateCustomTemplateAsync(Guid companyId, string name, string category, string description, string content, string userId, string? moduleType = null);
    Task<string> ReplaceVariablesAsync(string content, Dictionary<string, string> data);

    string GetModuleKeyByName(string moduleName);
    Task<string> ResolveModuleTypeAsync(int? moduleId);
    Task<SmsTemplate?> GetTemplateAsync(int id);
}

public class SmsTemplateService : ISmsTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SmsTemplateService> _logger;
    public SmsTemplateService(ApplicationDbContext context, ILogger<SmsTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasSmsIntegrationAsync(Guid companyId)
    {
        try
        {
            // Check if company has NetGSM integration configured and active
            var smsIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "SMS" &&
                                         ci.IsActive);

            if (smsIntegration == null)
                return false;

            // Validate required SMS settings
            var requiredFields = new[] { "username", "password" };
            foreach (var field in requiredFields)
            {
                if (!smsIntegration.Settings.ContainsKey(field) || string.IsNullOrWhiteSpace(smsIntegration.Settings[field]?.ToString()))
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SMS integration for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<List<SmsTemplate>> GetSystemTemplatesAsync(Guid companyId)
    {
        try
        {
            // Get company's purchased modules
            var companyModuleIds = await _context.CompanyModules
                .Where(cm => cm.CompanyId == companyId && cm.IsActive)
                .Select(cm => cm.ModuleId)
                .ToListAsync();

            // Get base templates that are either public (ModuleId is null) or company has the required module
            var baseTemplates = await _context.SmsTemplates
                .Where(st => st.IsActive && (st.ModuleId == null || companyModuleIds.Contains(st.ModuleId.Value)))
                .OrderBy(st => st.Category)
                .ThenBy(st => st.SortOrder)
                .ToListAsync();

            return baseTemplates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system SMS templates for company {CompanyId}", companyId);
            return new List<SmsTemplate>();
        }
    }

    public async Task<List<CompanySmsTemplate>> GetUserTemplatesAsync(Guid companyId)
    {
        var companyTemplates = await _context.CompanySmsTemplates
            .Where(cst => cst.CompanyId == companyId && cst.IsEnabled)
            .ToListAsync();

        return companyTemplates;
    }

    public async Task<bool> CopySystemTemplateToCompanyAsync(Guid companyId, int systemTemplateId, string userId)
    {
        try
        {
            // Check if already exists
            var existing = await _context.CompanySmsTemplates
                .AnyAsync(cst => cst.CompanyId == companyId && cst.SmsTemplateId == systemTemplateId);
            
            if (existing) return false; // Already have it

            var systemTemplate = await _context.SmsTemplates.FindAsync(systemTemplateId);
            if (systemTemplate == null) return false;

            // Prepare module type
            string moduleType = await ResolveModuleTypeAsync(systemTemplate.ModuleId);

            var newTemplate = new CompanySmsTemplate
            {
                CompanyId = companyId,
                SmsTemplateId = systemTemplateId,
                CustomContent = systemTemplate.DefaultContent,
                Name = systemTemplate.Name,
                Category = systemTemplate.Category,
                Description = systemTemplate.Description,
                ModuleType = moduleType,
                LastModifiedBy = userId,
                IsEnabled = true
            };

            await _context.CompanySmsTemplates.AddAsync(newTemplate);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying system template {TemplateId} for company {CompanyId}", systemTemplateId, companyId);
            return false;
        }
    }

    public async Task<CompanySmsTemplate?> GetCompanyTemplateAsync(Guid companyId, int companyTemplateId)
    {
        try
        {
            var template = await _context.CompanySmsTemplates
                .Include(cst => cst.SmsTemplate)
                .Include(cst => cst.LastModifiedByUser)
                .FirstOrDefaultAsync(cst => cst.CompanyId == companyId && cst.Id == companyTemplateId);

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company SMS template for company {CompanyId}, companyTemplateId {CompanyTemplateId}", companyId, companyTemplateId);
            return null;
        }
    }

    public async Task<bool> SaveCompanyTemplateAsync(Guid companyId, int? companyTemplateId, string content, string userId)
    {
        try
        {
            if (companyTemplateId.HasValue)
            {
                // Update existing company template by its ID
                var existingTemplate = await _context.CompanySmsTemplates
                    .FirstOrDefaultAsync(cst => cst.CompanyId == companyId && cst.Id == companyTemplateId.Value);

                if (existingTemplate != null)
                {
                    existingTemplate.CustomContent = content;
                    existingTemplate.LastModifiedBy = userId;
                    existingTemplate.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return true;
                }
                
                return false; // Template not found
            }
            else
            {
                // This shouldn't happen in the new architecture
                // All saves should have a companyTemplateId
                _logger.LogWarning("SaveCompanyTemplateAsync called without companyTemplateId for company {CompanyId}", companyId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving company SMS template for company {CompanyId}, companyTemplateId {CompanyTemplateId}", companyId, companyTemplateId);
            return false;
        }
    }

    public async Task<Dictionary<string, List<CompanySmsTemplate>>> GetGroupedTemplatesAsync(Guid companyId, string userId)
    {
        try
        {
            // Now only fetching user's templates (custom or imported/customized system templates)
            var templates = await GetUserTemplatesAsync(companyId);

            return templates
                .GroupBy(t => t.ModuleType)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grouped SMS templates for company {CompanyId}", companyId);
            return new Dictionary<string, List<CompanySmsTemplate>>();
        }
    }

    public async Task CreateCustomTemplateAsync(Guid companyId, string name, string category, string description, string content, string userId, string? moduleType = null)
    {
        try
        {
            // Store metadata in content with a special format
            // Resolve module type

            var companyTemplate = new CompanySmsTemplate
            {
                CompanyId = companyId,
                SmsTemplateId = null, // null for custom templates
                Name = name,
                Category = category,
                Description = description,
                CustomContent = content,
                ModuleType = moduleType,
                LastModifiedBy = userId,
                IsEnabled = true
            };

            await _context.CompanySmsTemplates.AddAsync(companyTemplate);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom SMS template for company {CompanyId}", companyId);
            throw;
        }
    }

    public Task<string> ReplaceVariablesAsync(string content, Dictionary<string, string> data)
    {
        var result = content;

        foreach (var kvp in data)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }

        return Task.FromResult(result);
    }

    #region Helper Methods

    public async Task<string> ResolveModuleTypeAsync(int? moduleId)
    {
        if (!moduleId.HasValue) return "";
        var module = await _context.Modules.FindAsync(moduleId.Value);
        if (module == null) return "";
        return GetModuleKeyByName(module.Name);
    }




    public async Task<SmsTemplate?> GetTemplateAsync(int id)
    {
        return await _context.SmsTemplates.FindAsync(id);
    }

    public string GetModuleKeyByName(string moduleName)
    {
        if (string.IsNullOrEmpty(moduleName)) return "";

        // Normalize string if needed
        var name = moduleName.Trim();

        if (name == "Sipariş Durumu Bildirimleri" || name == "Sipariş") return "OrderStatus";
        if (name == "Sepet Hatırlatma" || name == "Sepet") return "BasketReminder";
        if (name == "Toplu Mesaj Gönderimi" || name == "Müşteri") return "BulkMessaging";
        if (name == "İlk Alışveriş Mesajı") return "FirstOrder";
        
        return "";
    }

    #endregion
}
