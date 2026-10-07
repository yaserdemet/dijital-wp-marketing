using PushDashboard.Models;

namespace PushDashboard.Services;

/// <summary>
/// Kullanıcı bildirimleri için servis arayüzü
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    /// Kullanıcının bildirimlerini getirir
    /// </summary>
    Task<List<UserNotificationDto>> GetUserNotificationsAsync(string userId, int limit = 10, bool unreadOnly = false);
    
    /// <summary>
    /// Okunmamış bildirim sayısını getirir
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId);
    
    /// <summary>
    /// Bildirimi okundu olarak işaretler
    /// </summary>
    Task<bool> MarkAsReadAsync(int notificationId, string userId);
    
    /// <summary>
    /// Tüm bildirimleri okundu olarak işaretler
    /// </summary>
    Task<int> MarkAllAsReadAsync(string userId);
    
    /// <summary>
    /// Yeni bildirim oluşturur
    /// </summary>
    Task<UserNotification> CreateNotificationAsync(CreateNotificationRequest request);
    
    /// <summary>
    /// Bir kullanıcıya bildirim gönderir (basit kullanım)
    /// </summary>
    Task<UserNotification> SendNotificationAsync(
        string userId, 
        string title, 
        string message, 
        NotificationType type = NotificationType.Info,
        string? actionUrl = null,
        Guid? companyId = null);
}

/// <summary>
/// Bildirim DTO
/// </summary>
public class UserNotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string IconColor { get; set; } = string.Empty;
    public string IconBackground { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Bildirim oluşturma isteği
/// </summary>
public class CreateNotificationRequest
{
    public string UserId { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public string? Icon { get; set; }
    public string? IconColor { get; set; }
    public string? IconBackground { get; set; }
    public string? ActionUrl { get; set; }
}
