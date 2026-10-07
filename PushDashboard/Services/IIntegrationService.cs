using PushDashboard.DTOs;
using PushDashboard.Models;

namespace PushDashboard.Services;

/// <summary>
/// Service for managing integrations
/// </summary>
public interface IIntegrationService
{
    // === Integration Management ===
    
    /// <summary>
    /// Gets all active integrations
    /// </summary>
    Task<List<Integration>> GetAllActiveIntegrationsAsync();
    
    /// <summary>
    /// Gets integrations grouped by category with company status
    /// </summary>
    Task<List<GroupedIntegrationsDto>> GetGroupedIntegrationsAsync(Guid companyId);
    
    /// <summary>
    /// Gets company's integrations
    /// </summary>
    Task<List<CompanyIntegration>> GetCompanyIntegrationsAsync(Guid companyId);
    
    /// <summary>
    /// Gets a specific company integration
    /// </summary>
    Task<CompanyIntegration?> GetCompanyIntegrationAsync(Guid companyId, int integrationId);
    
    /// <summary>
    /// Adds an integration to a company
    /// Throws NotFoundException if integration doesn't exist
    /// Throws DuplicateException if integration already added
    /// </summary>
    Task<CompanyIntegration> AddIntegrationAsync(Guid companyId, int integrationId);
    
    // === Settings Management ===
    
    /// <summary>
    /// Gets integration settings for a company
    /// Throws NotFoundException if integration not found
    /// </summary>
    Task<Dictionary<string, object>> GetIntegrationSettingsAsync(Guid companyId, int integrationId);
    
    /// <summary>
    /// Saves integration settings and handles webhook setup for e-commerce integrations
    /// Throws NotFoundException if integration not found
    /// Returns success message
    /// </summary>
    Task<string> SaveIntegrationSettingsAsync(
        Guid companyId, 
        int integrationId, 
        Dictionary<string, object> settings,
        string userId,
        string scheme,
        string host);
    
    // === Connection Testing ===
    
    /// <summary>
    /// Tests connection for an integration
    /// </summary>
    Task<ConnectionTestResult> TestConnectionAsync(
        int integrationId, 
        Dictionary<string, object> settings,
        Guid? companyId = null);
    
    // === Status Mappings (Order Status Module) ===
    
    /// <summary>
    /// Checks if company has Order Status Notifications module
    /// </summary>
    Task<bool> HasOrderStatusModuleAsync(Guid companyId);
    
    /// <summary>
    /// Gets status mappings for an integration
    /// Throws NotFoundException if integration not found
    /// </summary>
    Task<Dictionary<string, string>> GetStatusMappingsAsync(
        Guid companyId, 
        int integrationId);
    
    /// <summary>
    /// Saves status mappings for an integration
    /// Throws ValidationException if module not available
    /// Throws NotFoundException if integration not found
    /// </summary>
    Task SaveStatusMappingsAsync(
        Guid companyId, 
        int integrationId, 
        Dictionary<string, string> mappings,
        string userId);
    
    /// <summary>
    /// Gets default Ticimax status mappings
    /// </summary>
    Dictionary<string, string> GetDefaultTicimaxMappings();
    
    // === Synchronization ===
    
    /// <summary>
    /// Synchronizes integration data (customers and baskets for e-commerce)
    /// Throws NotFoundException if integration not found
    /// Throws ValidationException if integration not configured
    /// </summary>
    Task<SyncResultData> SyncIntegrationAsync(
        Guid companyId, 
        int integrationId);
    
    // === Utility ===
    
    /// <summary>
    /// Gets active company integrations for listing
    /// </summary>
    Task<List<IntegrationDto>> GetActiveCompanyIntegrationsAsync(Guid companyId);
    
    /// <summary>
    /// Generates module embed code for a company
    /// </summary>
    Task<string> GenerateModuleEmbedCodeAsync(Guid companyId);
}

