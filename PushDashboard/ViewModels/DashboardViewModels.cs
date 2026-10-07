using PushDashboard.Models;

namespace PushDashboard.ViewModels;

public class DashboardIndexViewModel
{
    public DashboardStatsViewModel Stats { get; set; } = new();
    public List<ModuleUsageOverviewViewModel> ModuleUsage { get; set; } = new();
    public List<IntegrationStatusViewModel> IntegrationStatus { get; set; } = new();
    public List<RecentActivityViewModel> RecentActivities { get; set; } = new();
    public List<NotificationMetricsViewModel> NotificationMetrics { get; set; } = new();
    public DashboardChartsViewModel Charts { get; set; } = new();
    public List<QuickActionViewModel> QuickActions { get; set; } = new();
    public SyncInfoViewModel SyncInfo { get; set; } = new();
    public List<ModuleFinancialStatsViewModel> ModuleRevenues { get; set; } = new();
}

public class DashboardStatsViewModel
{
    public int ActiveModules { get; set; }
    public int PassiveModules { get; set; }
    public decimal CreditBalance { get; set; }
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int PassiveCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int NotificationsSentToday { get; set; }
    public int NotificationsSentThisMonth { get; set; }
    public decimal MonthlySpending { get; set; }
    public int ActiveIntegrations { get; set; }
    public double AverageDeliveryRate { get; set; }
    public DateTime? LastActivity { get; set; }

    // Formatted properties
    public string FormattedCreditBalance => $"₺{CreditBalance:N2}";
    public int FormattedActiveModules => ActiveModules;

    public double FormattedActiveModulesRatio
    {
        get
        {
            var total = ActiveModules + PassiveModules;
            if (total <= 0) return 0;

            var pct = (double)ActiveModules / total * 100.0;

            return Math.Clamp(pct, 0, 100);
        }
    }
    public double ActiveCustomersRatio => TotalCustomers > 0 ? (double)ActiveCustomers / TotalCustomers * 100 : 0;
    public double PassiveCustomersRatio => TotalCustomers > 0 ? (double)PassiveCustomers / TotalCustomers * 100 : 0;
    public double NewCustomersRatio => TotalCustomers > 0 ? (double)NewCustomers / TotalCustomers * 100 : 0;

    public string FormattedMonthlySpending => $"₺{MonthlySpending:N2}";
    public string FormattedDeliveryRate => $"{AverageDeliveryRate:F1}%";
    public string FormattedLastActivity => FormatLastActivity(LastActivity);

    private string FormatLastActivity(DateTime? lastActivity)
    {
        if (!lastActivity.HasValue || lastActivity.Value <= DateTime.MinValue)
            return "Henüz aktivite yok";

        var turkishMonths = new[] { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
            "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

        var monthIndex = lastActivity.Value.Month - 1;
        var monthName = turkishMonths[monthIndex];

        return $"{lastActivity.Value:dd} {monthName} {lastActivity.Value:yyyy} {lastActivity.Value:HH:mm}";
    }
}

public class ModuleUsageOverviewViewModel
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string? ModuleIcon { get; set; }
    public string? ModuleColor { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCountThisMonth { get; set; }
    public decimal CostThisMonth { get; set; }
    public double SuccessRate { get; set; }

    public string FormattedCost => $"₺{CostThisMonth:N2}";
    public string FormattedSuccessRate => $"{SuccessRate:F1}%";
    public string FormattedLastUsed => LastUsed?.ToString("dd MMM") ?? "Hiç kullanılmadı";
    public string StatusBadgeClass => IsActive ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800";
    public string StatusText => IsActive ? "Aktif" : "Pasif";
}

public class IntegrationStatusViewModel
{
    public int IntegrationId { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public string IntegrationType { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSync { get; set; }
    public string? LastError { get; set; }
    public int SuccessfulOperationsToday { get; set; }
    public int FailedOperationsToday { get; set; }

    // Service-triggered deactivation tracking
    public bool DeactivatedByService { get; set; }
    public string? DeactivationReason { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    /// <summary>
    /// Durum metni - Bağlı, Yapılandırılmadı, Pasif, Bağlantı Hatası
    /// </summary>
    public string StatusText
    {
        get
        {
            if (!IsConnected)
                return "Yapılandırılmadı";
            if (DeactivatedByService)
                return "Başarısız";
            if (!IsActive)
                return "Pasif";
            if (!string.IsNullOrEmpty(LastError))
                return "Hata";
            return "Bağlı";
        }
    }

    /// <summary>
    /// Durum badge CSS sınıfı
    /// </summary>
    public string StatusBadgeClass
    {
        get
        {
            if (!IsConnected)
                return "bg-gray-100 text-gray-800";
            if (!IsActive)
                return "bg-yellow-100 text-yellow-800";
            if (!string.IsNullOrEmpty(LastError))
                return "bg-red-100 text-red-800";
            return "bg-green-100 text-green-800";
        }
    }

    /// <summary>
    /// Son senkronizasyon zamanı formatlanmış
    /// </summary>
    public string FormattedLastSync
    {
        get
        {
            if (!LastSync.HasValue)
                return "Henüz senkronize edilmedi";

            var diff = DateTime.Now - LastSync.Value;

            if (diff.TotalMinutes < 1)
                return "Az önce";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} dakika önce";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} saat önce";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} gün önce";

            return LastSync.Value.ToString("dd MMM HH:mm");
        }
    }

    public double SuccessRate => (SuccessfulOperationsToday + FailedOperationsToday) > 0
        ? (double)SuccessfulOperationsToday / (SuccessfulOperationsToday + FailedOperationsToday) * 100
        : 0;
    public string FormattedSuccessRate => $"{SuccessRate:F1}%";
}

public class RecentActivityViewModel
{
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? ModuleName { get; set; }
    public string? Status { get; set; }
    public decimal? Cost { get; set; }

    public string FormattedTimestamp => Timestamp.ToString("dd MMM HH:mm");
    public string FormattedCost => Cost.HasValue ? $"₺{Cost.Value:N2}" : "";
    public string ActivityIcon => ActivityType switch
    {
        "notification" => "bell",
        "module_purchase" => "shopping-cart",
        "integration" => "link",
        "sync" => "refresh-cw",
        _ => "activity"
    };
}

public class NotificationMetricsViewModel
{
    public string ChannelType { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public int SentToday { get; set; }
    public int SentThisWeek { get; set; }
    public int SentThisMonth { get; set; }
    public int DeliveredToday { get; set; }
    public int FailedToday { get; set; }
    public decimal CostToday { get; set; }
    public decimal CostThisMonth { get; set; }

    public double DeliveryRateToday => SentToday > 0 ? (double)DeliveredToday / SentToday * 100 : 0;
    public string FormattedDeliveryRate => $"{DeliveryRateToday:F1}%";
    public string FormattedCostToday => $"₺{CostToday:N2}";
    public string FormattedCostThisMonth => $"₺{CostThisMonth:N2}";
    public string ChannelIcon => ChannelType.ToLowerInvariant() switch
    {
        "email" => "mail",
        "whatsapp" => "message-circle",
        "sms" => "smartphone",
        "hediye_çarkı" or "hediye_çarki" => "gift",
        "video_yükleme" or "video_yukleme" => "video",
        "trendyol" => "shopping-bag",
        "çoklu_kanal" or "coklu_kanal" => "layers",
        _ => "send"
    };
}

public class DashboardChartsViewModel
{
    public List<ChartDataPoint> UsageTrend { get; set; } = new();
    public List<ChartDataPoint> CostTrend { get; set; } = new();
    public List<ChartDataPoint> DeliveryRates { get; set; } = new();
    public List<PieChartDataPoint> ModuleDistribution { get; set; } = new();
    public List<PieChartDataPoint> ChannelDistribution { get; set; } = new();
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime Date { get; set; }
    public string? Category { get; set; }
}

public class PieChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Color { get; set; } = string.Empty;
    public double Percentage { get; set; }
}

public class QuickActionViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty; // "link", "button", "modal"
    public bool IsEnabled { get; set; } = true;
    public string? Badge { get; set; }
    public string? BadgeColor { get; set; }
}

public class SyncInfoViewModel
{
    public DateTime? LastSyncAt { get; set; }
    public DateTime? LastUpdateAt { get; set; }

    public string FormattedLastSync => LastSyncAt?.ToString("dd.MM.yyyy HH:mm:ss") ?? "Henüz senkronize edilmedi";
    public string FormattedLastUpdate => LastUpdateAt?.ToString("dd.MM.yyyy HH:mm:ss") ?? "Henüz güncellenmedi";
    public string TimeAgoLastSync => GetTimeAgo(LastSyncAt);
    public string TimeAgoLastUpdate => GetTimeAgo(LastUpdateAt);

    private static string GetTimeAgo(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return "Henüz yapılmadı";

        var timeSpan = DateTime.Now - dateTime.Value;

        if (timeSpan.TotalSeconds < 60)
            return "Az önce";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} dakika önce";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} saat önce";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} gün önce";

        return dateTime.Value.ToString("dd.MM.yyyy");
    }
}

public class ModuleRevenueViewModel
{
    public string ModuleName { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public string FormattedRevenue => $"₺{TotalRevenue:N2}";
}

public class ModuleFinancialStatsViewModel
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenditure { get; set; }
    public decimal NetIncome => TotalRevenue - TotalExpenditure;
    
    public string FormattedRevenue => $"₺{TotalRevenue:N2}";
    public string FormattedExpenditure => $"₺{TotalExpenditure:N2}";
    public string FormattedNetIncome => $"₺{NetIncome:N2}";
    
    // Revenue details (Templates)
    public List<ModuleRevenueViewModel> Templates { get; set; } = new();
}
