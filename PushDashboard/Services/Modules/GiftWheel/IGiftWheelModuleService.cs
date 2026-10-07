using PushDashboard.DTOs;

namespace PushDashboard.Services.Modules.GiftWheel;

/// <summary>
/// Service interface for Gift Wheel module management (following existing module patterns)
/// </summary>
public interface IGiftWheelModuleService
{
    #region Module Settings

    /// <summary>
    /// Gets the gift wheel module settings for a company
    /// </summary>
    Task<GiftWheelModuleSettingsDto> GetModuleSettingsAsync(Guid companyId);

    /// <summary>
    /// Updates the gift wheel module settings for a company
    /// </summary>
    Task<(bool Success, string Message)> UpdateModuleSettingsAsync(Guid companyId, GiftWheelModuleSettingsDto settings, string userId);

    #endregion

    #region Module Statistics

    /// <summary>
    /// Gets comprehensive module statistics for the company
    /// </summary>
    Task<GiftWheelModuleStatsDto> GetModuleStatsAsync(Guid companyId);

    /// <summary>
    /// Gets module usage history for billing and tracking
    /// </summary>
    Task<List<ModuleUsageDto>> GetModuleUsageHistoryAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);

    #endregion

    #region Module Status

    /// <summary>
    /// Checks if the company has an active gift wheel module
    /// </summary>
    Task<bool> HasActiveModuleAsync(Guid companyId);

    /// <summary>
    /// Gets the module activation status and details
    /// </summary>
    Task<ModuleStatusDto> GetModuleStatusAsync(Guid companyId);

    /// <summary>
    /// Validates if the company can use gift wheel features
    /// </summary>
    Task<(bool CanUse, string Message, decimal RequiredCredits)> ValidateModuleUsageAsync(Guid companyId, string operationType);

    #endregion

    #region Integration Checks

    /// <summary>
    /// Checks if company has required integrations for gift wheel functionality
    /// </summary>
    Task<IntegrationStatusDto> CheckRequiredIntegrationsAsync(Guid companyId);

    /// <summary>
    /// Gets the active e-commerce platform for voucher creation
    /// </summary>
    Task<string?> GetActiveEcommercePlatformAsync(Guid companyId);

    /// <summary>
    /// Checks if WhatsApp notifications are configured
    /// </summary>
    Task<bool> IsWhatsAppConfiguredAsync(Guid companyId);

    #endregion
}
