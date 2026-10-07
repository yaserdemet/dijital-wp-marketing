using PushDashboard.DTOs;

namespace PushDashboard.Services.Modules.VideoHosting;

public interface IVideoHostingModuleService
{
    #region Module Status

    /// <summary>
    /// Checks if the company has an active video hosting module
    /// </summary>
    Task<bool> HasActiveModuleAsync(Guid companyId);

    /// <summary>
    /// Gets the module activation status and details
    /// </summary>
    Task<ModuleStatusDto> GetModuleStatusAsync(Guid companyId);

    /// <summary>
    /// Validates if the company can use video hosting features
    /// </summary>
    Task<(bool CanUse, string Message, decimal RequiredCredits)> ValidateModuleUsageAsync(Guid companyId, string operationType);

    #endregion

    #region Module Settings

    /// <summary>
    /// Gets module settings for the company
    /// </summary>
    Task<VideoSettingsDto?> GetModuleSettingsAsync(Guid companyId);

    /// <summary>
    /// Updates module settings for the company
    /// </summary>
    Task<(bool Success, string Message)> UpdateModuleSettingsAsync(
        Guid companyId, 
        VideoSettingsRequestDto settings, 
        string userId);

    /// <summary>
    /// Resets module settings to default values
    /// </summary>
    Task<(bool Success, string Message)> ResetModuleSettingsAsync(Guid companyId, string userId);

    #endregion

    #region Module Usage

    /// <summary>
    /// Gets module usage history for billing and tracking
    /// </summary>
    Task<List<ModuleUsageDto>> GetModuleUsageHistoryAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);

    /// <summary>
    /// Gets module usage statistics
    /// </summary>
    Task<ModuleUsageStatsDto> GetModuleUsageStatsAsync(Guid companyId);

    #endregion

    #region Module Analytics

    /// <summary>
    /// Gets comprehensive module analytics
    /// </summary>
    Task<VideoHostingAnalyticsDto> GetModuleAnalyticsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null);

    #endregion
}

/// <summary>
/// Module status information
/// </summary>
public class ModuleStatusDto
{
    public bool IsActive { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal PaidAmount { get; set; }
    public string PurchasedByUserName { get; set; } = string.Empty;
    public bool HasSettings { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// Module usage information
/// </summary>
public class ModuleUsageDto
{
    public int Id { get; set; }
    public string UsageType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public bool IsSuccessful { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ReferenceId { get; set; }
}

/// <summary>
/// Module usage statistics
/// </summary>
public class ModuleUsageStatsDto
{
    public int TotalUsageCount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AverageCostPerUsage { get; set; }
    public DateTime? FirstUsageDate { get; set; }
    public DateTime? LastUsageDate { get; set; }
    public int SuccessfulUsageCount { get; set; }
    public int FailedUsageCount { get; set; }
    public decimal SuccessRate { get; set; }
}

/// <summary>
/// Comprehensive video hosting analytics
/// </summary>
public class VideoHostingAnalyticsDto
{
    public VideoStatsDto VideoStats { get; set; } = new();
    public ModuleUsageStatsDto UsageStats { get; set; } = new();
    public List<DailyUsageDto> DailyUsage { get; set; } = new();
    public List<VideoPerformanceDto> TopVideos { get; set; } = new();
    public StorageAnalyticsDto StorageAnalytics { get; set; } = new();
}

/// <summary>
/// Daily usage statistics
/// </summary>
public class DailyUsageDto
{
    public DateTime Date { get; set; }
    public int VideoUploads { get; set; }
    public int TotalViews { get; set; }
    public decimal TotalCost { get; set; }
    public long StorageUsed { get; set; }
}

/// <summary>
/// Video performance metrics
/// </summary>
public class VideoPerformanceDto
{
    public Guid VideoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal EngagementRate { get; set; }
}

/// <summary>
/// Storage analytics
/// </summary>
public class StorageAnalyticsDto
{
    public long TotalStorageBytes { get; set; }
    public string TotalStorageFormatted { get; set; } = string.Empty;
    public decimal MonthlyStorageCost { get; set; }
    public long AverageVideoSize { get; set; }
    public string LargestVideoTitle { get; set; } = string.Empty;
    public long LargestVideoSize { get; set; }
}
