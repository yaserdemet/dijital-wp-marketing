namespace PushDashboard.Models.ViewModels;

/// <summary>
/// Cookie Management ana sayfa için view model
/// </summary>
public class CookieManagementIndexViewModel
{
    public CookieManagement Settings { get; set; } = null!;
    public CookieManagementStatsViewModel Stats { get; set; } = new();
}

/// <summary>
/// Cookie Management istatistik view model
/// </summary>
public class CookieManagementStatsViewModel
{
    public int TotalImpressions { get; set; }
    public int TotalAcceptAll { get; set; }
    public int TotalRejectAll { get; set; }
    public int TotalSaveSettings { get; set; }
    public double AcceptRate { get; set; }
    public double RejectRate { get; set; }
    public int MobileCount { get; set; }
    public int DesktopCount { get; set; }
    public List<CookieCategoryStatsViewModel> CategoryStats { get; set; } = new();
}

/// <summary>
/// Kategori bazlı kabul istatistiği
/// </summary>
public class CookieCategoryStatsViewModel
{
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int AcceptCount { get; set; }
    public int TotalEvents { get; set; }
    public double AcceptRate { get; set; }
}

/// <summary>
/// Cookie tracking event gönderimi için input model
/// </summary>
public class CookieManagementTrackingViewModel
{
    public Guid CompanyId { get; set; }
    public string EventType { get; set; } = "impression";
    public List<string>? AcceptedCategories { get; set; }
    public string? PageUrl { get; set; }
    public string? SessionId { get; set; }
}
