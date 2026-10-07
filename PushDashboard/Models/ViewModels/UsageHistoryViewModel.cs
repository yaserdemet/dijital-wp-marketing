using PushDashboard.Services;

namespace PushDashboard.Models.ViewModels;

public class UsageHistoryViewModel
{
    public List<ModuleUsageLog> UsageHistory { get; set; } = new();
    public ModuleUsageStats UsageStats { get; set; } = new();
    public decimal CurrentBalance { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
