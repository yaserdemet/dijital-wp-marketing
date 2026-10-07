using PushDashboard.Models;

namespace PushDashboard.Services;

public interface IActivityLogService
{
    /// <summary>
    /// Yeni bir aktivite logu oluşturur
    /// </summary>
    Task LogActivityAsync(
        string userId,
        Guid companyId,
        string actionType,
        string module,
        string description,
        string? entityId = null,
        string? entityType = null,
        object? details = null,
        string? ipAddress = null,
        string? userAgent = null);

    /// <summary>
    /// Şirkete ait aktivite loglarını getirir
    /// </summary>
    Task<(List<ActivityLogViewModel> Logs, int TotalCount)> GetActivityLogsAsync(
        Guid companyId,
        int page = 1,
        int pageSize = 20,
        string? module = null,
        string? actionType = null,
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchTerm = null);

    /// <summary>
    /// Kullanıcıya ait aktivite loglarını getirir
    /// </summary>
    Task<List<ActivityLogViewModel>> GetUserActivityLogsAsync(
        string userId,
        int limit = 50);

    /// <summary>
    /// İstatistikleri getirir
    /// </summary>
    Task<ActivityLogStatsViewModel> GetActivityStatsAsync(Guid companyId, int lastDays = 30);
}

public class ActivityLogViewModel
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string ActionTypeDisplay { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
    public string ActionIcon { get; set; } = string.Empty;
    public string ActionColor { get; set; } = string.Empty;
}

public class ActivityLogStatsViewModel
{
    public int TotalActions { get; set; }
    public int TodayActions { get; set; }
    public int UniqueUsers { get; set; }
    public Dictionary<string, int> ActionsByType { get; set; } = new();
    public Dictionary<string, int> ActionsByModule { get; set; } = new();
    public List<DailyActivityViewModel> DailyActivity { get; set; } = new();
}

public class DailyActivityViewModel
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

