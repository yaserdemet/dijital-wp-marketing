using PushDashboard.DTOs;
using PushDashboard.Models;

namespace PushDashboard.Services.Modules.GiftWheel;

/// <summary>
/// Service interface for Gift Wheel functionality
/// </summary>
public interface IGiftWheelService
{
    #region Wheel Management

    /// <summary>
    /// Gets the company's gift wheel configuration
    /// </summary>
    Task<Models.GiftWheel?> GetCompanyWheelAsync(Guid companyId);

    /// <summary>
    /// Creates or updates a company's gift wheel
    /// </summary>
    Task<(bool Success, string Message)> CreateOrUpdateWheelAsync(Guid companyId, string wheelName, string userId);

    /// <summary>
    /// Activates or deactivates a gift wheel
    /// </summary>
    Task<(bool Success, string Message)> ToggleWheelStatusAsync(Guid companyId, bool isActive, string userId);

    #endregion

    #region Prize Management

    /// <summary>
    /// Gets all prizes for a wheel
    /// </summary>
    Task<List<GiftWheelPrizeDto>> GetWheelPrizesAsync(int wheelId);

    /// <summary>
    /// Adds a new prize to the wheel
    /// </summary>
    Task<(bool Success, string Message, GiftWheelPrizeDto? Prize)> AddPrizeAsync(int wheelId, GiftWheelPrizeRequest request);

    /// <summary>
    /// Updates an existing prize
    /// </summary>
    Task<(bool Success, string Message)> UpdatePrizeAsync(int prizeId, GiftWheelPrizeRequest request);

    /// <summary>
    /// Deletes a prize from the wheel
    /// </summary>
    Task<(bool Success, string Message)> DeletePrizeAsync(int prizeId);

    /// <summary>
    /// Deletes all prizes from the wheel
    /// </summary>
    Task<(bool Success, string Message)> DeleteAllPrizesAsync(int wheelId);

    /// <summary>
    /// Reorders prizes in the wheel
    /// </summary>
    Task<(bool Success, string Message)> ReorderPrizesAsync(int wheelId, Dictionary<int, int> prizeOrders);

    /// <summary>
    /// Toggles the status of a specific prize
    /// </summary>
    Task<(bool Success, string Message)> TogglePrizeStatusAsync(int prizeId, bool isActive);

    /// <summary>
    /// Toggles the status of all prizes in a wheel
    /// </summary>
    Task<(bool Success, string Message)> ToggleAllPrizesAsync(int wheelId, bool isActive);

    #endregion

    #region Settings Management

    /// <summary>
    /// Gets wheel settings
    /// </summary>
    Task<GiftWheelSettingsDto?> GetWheelSettingsAsync(int wheelId);

    /// <summary>
    /// Updates wheel settings
    /// </summary>
    Task<(bool Success, string Message)> UpdateWheelSettingsAsync(int wheelId, UpdateGiftWheelSettingsRequest request, string userId);

    #endregion

    #region Spin Processing

    /// <summary>
    /// Processes a wheel spin request
    /// </summary>
    Task<GiftWheelSpinResult> ProcessSpinAsync(Guid companyId, GiftWheelSpinRequest request, string ipAddress);

    /// <summary>
    /// Validates if a customer can spin the wheel
    /// </summary>
    Task<(bool CanSpin, string Message)> ValidateSpinEligibilityAsync(Guid companyId, string customerPhone, string ipAddress);

    /// <summary>
    /// Determines the winning prize based on probabilities
    /// </summary>
    Task<GiftWheelPrize?> DetermineWinningPrizeAsync(int wheelId);

    /// <summary>
    /// Creates a gift voucher for the winning customer
    /// </summary>
    Task<(bool Success, string? VoucherCode, string Message)> CreateGiftVoucherAsync(Guid companyId, Customer customer, GiftWheelPrize prize);

    /// <summary>
    /// Sends notification to the customer about their prize
    /// </summary>
    Task<(bool Success, string Message)> SendPrizeNotificationAsync(Guid companyId, Models.GiftWheelSpin spin);

    #endregion

    #region Statistics and Reporting

    /// <summary>
    /// Gets comprehensive wheel statistics
    /// </summary>
    Task<GiftWheelStatsDto> GetWheelStatsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets recent spins for the wheel
    /// </summary>
    Task<List<RecentSpinDto>> GetRecentSpinsAsync(Guid companyId, int limit = 50);

    /// <summary>
    /// Gets daily spin statistics for charts
    /// </summary>
    Task<List<DailySpinStatsDto>> GetDailyStatsAsync(Guid companyId, int days = 30);

    /// <summary>
    /// Gets dashboard KPI data
    /// </summary>
    Task<GiftWheelDashboardKpiDto> GetDashboardKpisAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets funnel statistics
    /// </summary>
    Task<GiftWheelFunnelDto> GetFunnelStatsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets trend statistics for charts
    /// </summary>
    Task<GiftWheelTrendDto> GetTrendStatsAsync(Guid companyId, int days = 30);

    /// <summary>
    /// Gets detailed spin logs with filtering and pagination
    /// </summary>
    Task<PaginatedSpinLogResultDto> GetDetailedLogsAsync(Guid companyId, SpinLogFilterDto filter);

    /// <summary>
    /// Processes voucher used webhook from Ticimax
    /// </summary>
    Task<(bool Success, string Message)> ProcessVoucherUsedWebhookAsync(Guid companyId, TicimaxVoucherWebhookDto webhookData);

    /// <summary>
    /// Syncs orders from Ticimax and updates voucher usage information
    /// </summary>
    Task<(bool Success, string Message, int UpdatedCount)> SyncOrdersFromTicimaxAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Processes a single order for gift wheel voucher usage (called from OrderCreated webhook)
    /// </summary>
    Task<(bool Success, string Message)> ProcessOrderForVoucherUsageAsync(Guid companyId, string orderId);

    #endregion

    #region Script Generation

    /// <summary>
    /// Generates the JavaScript embed script for the wheel
    /// </summary>
    Task<string> GenerateEmbedScriptAsync(Guid companyId);

    /// <summary>
    /// Gets wheel configuration for the embed script
    /// </summary>
    Task<GiftWheelConfigDto?> GetWheelConfigAsync(Guid companyId);

    #endregion

    #region Utility Methods

    /// <summary>
    /// Checks if company has an active gift wheel module
    /// </summary>
    Task<bool> HasActiveModuleAsync(Guid companyId);

    /// <summary>
    /// Checks if company has sufficient credits for wheel operations
    /// </summary>
    Task<(bool HasCredits, decimal RequiredAmount, decimal AvailableBalance)> CheckCreditsAsync(Guid companyId, decimal requiredAmount);

    /// <summary>
    /// Logs wheel usage and deducts costs
    /// </summary>
    Task<(bool Success, string Message)> LogUsageAndDeductCostAsync(Guid companyId, string usageType, decimal cost, string description, string userId, string? referenceId = null);

    #endregion

    #region R2 Export

    /// <summary>
    /// Exports gift wheel configuration to R2 storage
    /// </summary>
    Task<string> ExportGiftWheelToR2Async(Guid companyId);

    #endregion
}
