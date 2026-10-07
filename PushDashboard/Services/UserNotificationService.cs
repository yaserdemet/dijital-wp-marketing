using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;

namespace PushDashboard.Services;

/// <summary>
/// Kullanıcı bildirimleri servisi
/// </summary>
public class UserNotificationService : IUserNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserNotificationService> _logger;

    public UserNotificationService(
        ApplicationDbContext context,
        ILogger<UserNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<UserNotificationDto>> GetUserNotificationsAsync(string userId, int limit = 10, bool unreadOnly = false)
    {
        var query = _context.UserNotifications
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new UserNotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Icon = n.Icon,
                IconColor = n.IconColor,
                IconBackground = n.IconBackground,
                IsRead = n.IsRead,
                ActionUrl = n.ActionUrl,
                CreatedAt = n.CreatedAt,
                TimeAgo = GetTimeAgo(n.CreatedAt)
            })
            .ToListAsync();

        return notifications;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.UserNotifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
    {
        var notification = await _context.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return false;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.Now;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Notification {NotificationId} marked as read for user {UserId}", notificationId, userId);
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(string userId)
    {
        var unreadNotifications = await _context.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Count == 0)
        {
            return 0;
        }

        var now = DateTime.Now;
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("{Count} notifications marked as read for user {UserId}", unreadNotifications.Count, userId);
        return unreadNotifications.Count;
    }

    public async Task<UserNotification> CreateNotificationAsync(CreateNotificationRequest request)
    {
        var (icon, iconColor, iconBackground) = GetIconConfig(request.Type, request.Icon, request.IconColor, request.IconBackground);

        var notification = new UserNotification
        {
            UserId = request.UserId,
            CompanyId = request.CompanyId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Icon = icon,
            IconColor = iconColor,
            IconBackground = iconBackground,
            ActionUrl = request.ActionUrl,
            CreatedAt = DateTime.Now
        };

        _context.UserNotifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created notification {NotificationId} for user {UserId}: {Title}",
            notification.Id, request.UserId, request.Title);

        return notification;
    }

    public async Task<UserNotification> SendNotificationAsync(
        string userId,
        string title,
        string message,
        NotificationType type = NotificationType.Info,
        string? actionUrl = null,
        Guid? companyId = null)
    {
        return await CreateNotificationAsync(new CreateNotificationRequest
        {
            UserId = userId,
            CompanyId = companyId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl
        });
    }

    private static (string icon, string iconColor, string iconBackground) GetIconConfig(
        NotificationType type,
        string? customIcon = null,
        string? customIconColor = null,
        string? customIconBackground = null)
    {
        if (!string.IsNullOrEmpty(customIcon) && !string.IsNullOrEmpty(customIconColor) && !string.IsNullOrEmpty(customIconBackground))
        {
            return (customIcon, customIconColor, customIconBackground);
        }

        return type switch
        {
            NotificationType.Success => (
                customIcon ?? "fas fa-check-circle",
                customIconColor ?? "text-green-500",
                customIconBackground ?? "bg-green-100"),
            NotificationType.Warning => (
                customIcon ?? "fas fa-exclamation-triangle",
                customIconColor ?? "text-yellow-500",
                customIconBackground ?? "bg-yellow-100"),
            NotificationType.Error => (
                customIcon ?? "fas fa-times-circle",
                customIconColor ?? "text-red-500",
                customIconBackground ?? "bg-red-100"),
            _ => (
                customIcon ?? "fas fa-info-circle",
                customIconColor ?? "text-blue-500",
                customIconBackground ?? "bg-blue-100")
        };
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "Az önce";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} dk önce";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} saat önce";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} gün önce";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} hafta önce";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} ay önce";

        return $"{(int)(timeSpan.TotalDays / 365)} yıl önce";
    }
}
