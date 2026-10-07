using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using PushDashboard.Models;

namespace PushDashboard.Hubs;

[Authorize]
public class SessionHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SessionHub> _logger;

    public SessionHub(UserManager<ApplicationUser> userManager, ILogger<SessionHub> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var user = await _userManager.GetUserAsync(Context.User);
        if (user != null)
        {
            // Add user to their personal group for session notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{user.Id}");
            _logger.LogInformation("User {UserId} connected to SessionHub with connection {ConnectionId}", user.Id, Context.ConnectionId);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = await _userManager.GetUserAsync(Context.User);
        if (user != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{user.Id}");
            _logger.LogInformation("User {UserId} disconnected from SessionHub with connection {ConnectionId}", user.Id, Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}
