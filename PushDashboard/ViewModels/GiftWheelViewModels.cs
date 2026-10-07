using PushDashboard.DTOs;
using PushDashboard.Models;

namespace PushDashboard.ViewModels;

/// <summary>
/// View model for gift wheel main page
/// </summary>
public class GiftWheelViewModel
{
    public Models.GiftWheel? Wheel { get; set; }
    public GiftWheelSettingsDto? Settings { get; set; }
    public List<GiftWheelPrizeDto> Prizes { get; set; } = new();
    public GiftWheelStatsDto Stats { get; set; } = new();
    public GiftWheelDashboardKpiDto Kpis { get; set; } = new();
    public IntegrationStatusDto IntegrationStatus { get; set; } = new();
    public Guid CompanyId { get; set; }
}

/// <summary>
/// Request model for creating/updating wheel
/// </summary>
public class CreateWheelRequest
{
    public string Name { get; set; } = "Hediye Çarkı";
}

/// <summary>
/// Request model for toggling wheel status
/// </summary>
public class ToggleWheelStatusRequest
{
    public bool IsActive { get; set; }
}

/// <summary>
/// Request model for toggling prize status
/// </summary>
public class TogglePrizeStatusRequest
{
    public bool IsActive { get; set; }
}

/// <summary>
/// Request model for toggling all prizes status
/// </summary>
public class ToggleAllPrizesRequest
{
    public bool IsActive { get; set; }
}

/// <summary>
/// Request model for reordering prizes
/// </summary>
public class ReorderPrizesRequest
{
    public Dictionary<int, int> PrizeOrders { get; set; } = new();
}
