using PushDashboard.DTOs;

namespace PushDashboard.Services.Modules.ProductSlider;

/// <summary>
/// Product Slider module access and management service
/// </summary>
public interface IProductSliderModuleService
{
    /// <summary>
    /// Checks if company has active Product Slider module
    /// </summary>
    Task<bool> HasActiveModuleAsync(Guid companyId);

    /// <summary>
    /// Gets module status information for the company
    /// </summary>
    Task<ModuleStatusDto> GetModuleStatusAsync(Guid companyId);

    /// <summary>
    /// Gets module settings for the company
    /// </summary>
    Task<Dictionary<string, object>?> GetModuleSettingsAsync(Guid companyId);

    /// <summary>
    /// Updates module settings for the company
    /// </summary>
    Task<(bool Success, string Message)> UpdateModuleSettingsAsync(Guid companyId, string userId, Dictionary<string, object> settings);
}
