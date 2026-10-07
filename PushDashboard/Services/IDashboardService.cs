using PushDashboard.ViewModels;

namespace PushDashboard.Services;

public interface IDashboardService
{
    /// <summary>
    /// Ana dashboard verilerini getirir
    /// </summary>
    Task<DashboardIndexViewModel> GetDashboardDataAsync(Guid companyId);

    /// <summary>
    /// Dashboard istatistiklerini getirir
    /// </summary>
    Task<DashboardStatsViewModel> GetDashboardStatsAsync(Guid companyId);

    /// <summary>
    /// Modül kullanım özetini getirir
    /// </summary>
    Task<List<ModuleUsageOverviewViewModel>> GetModuleUsageOverviewAsync(Guid companyId);

    /// <summary>
    /// Entegrasyon durumlarını getirir
    /// </summary>
    Task<List<IntegrationStatusViewModel>> GetIntegrationStatusAsync(Guid companyId);

    /// <summary>
    /// Son aktiviteleri getirir
    /// </summary>
    Task<List<RecentActivityViewModel>> GetRecentActivitiesAsync(Guid companyId, int limit = 10);

    /// <summary>
    /// Bildirim metriklerini getirir
    /// </summary>
    Task<List<NotificationMetricsViewModel>> GetNotificationMetricsAsync(Guid companyId);

    /// <summary>
    /// Dashboard grafik verilerini getirir
    /// </summary>
    Task<DashboardChartsViewModel> GetDashboardChartsAsync(Guid companyId);

    /// <summary>
    /// Belirtilen gün sayısı için kullanım trendi verilerini getirir
    /// </summary>
    Task<DashboardChartsViewModel> GetDashboardChartsAsync(Guid companyId, int days);

    /// <summary>
    /// Hızlı işlem kartlarını getirir
    /// </summary>
    Task<List<QuickActionViewModel>> GetQuickActionsAsync(Guid companyId);

    /// <summary>
    /// Gerçek zamanlı dashboard verilerini getirir (AJAX için)
    /// </summary>
    Task<object> GetRealTimeDataAsync(Guid companyId);

    /// <summary>
    /// Modül bazlı ciro ve harcama verilerini getirir
    /// </summary>
    Task<List<ModuleFinancialStatsViewModel>> GetModuleRevenuesAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null);
}
