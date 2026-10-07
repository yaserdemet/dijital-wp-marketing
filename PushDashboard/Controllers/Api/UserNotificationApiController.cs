using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.Filters;
using PushDashboard.Services;
using System.Security.Claims;
using PushDashboard.Data;

namespace PushDashboard.Controllers.Api;

/// <summary>
/// Kullanıcı bildirimleri API controller'ı
/// </summary>
[ApiController]
[Route("api/user-notifications")]
[Authorize]
[RequireUser]
public class UserNotificationApiController : BaseController
{
    private readonly IUserNotificationService _notificationService;

    public UserNotificationApiController(
        IUserNotificationService notificationService,
        IUserContextService userContextService,
        ApplicationDbContext context) : base(userContextService, context)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Kullanıcının bildirimlerini getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int limit = 10, [FromQuery] bool unreadOnly = false)
    {
        var userId = GetCurrentUserId()!;
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, limit, unreadOnly);
        return Ok(new { success = true, data = notifications });
    }

    /// <summary>
    /// Okunmamış bildirim sayısını getirir
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId()!;
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { success = true, count });
    }

    /// <summary>
    /// Bildirimi okundu olarak işaretler
    /// </summary>
    [HttpPost("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId()!;
        var result = await _notificationService.MarkAsReadAsync(id, userId);
        if (!result)
        {
            return NotFound(new { success = false, message = "Bildirim bulunamadı." });
        }

        return Ok(new { success = true, message = "Bildirim okundu olarak işaretlendi." });
    }

    /// <summary>
    /// Tüm bildirimleri okundu olarak işaretler
    /// </summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId()!;
        var count = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { success = true, message = $"{count} bildirim okundu olarak işaretlendi.", count });
    }
}
