using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using PushDashboard.Models;

namespace PushDashboard.Hubs;

[Authorize]
public class CustomerImportHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerImportHub(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task JoinCompanyGroup()
    {
        var user = await _userManager.GetUserAsync(Context.User);
        if (user?.CompanyId != null)
        {
            var groupName = $"Company_{user.CompanyId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
    }

    public async Task LeaveCompanyGroup()
    {
        var user = await _userManager.GetUserAsync(Context.User);
        if (user?.CompanyId != null)
        {
            var groupName = $"Company_{user.CompanyId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await JoinCompanyGroup();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveCompanyGroup();
        await base.OnDisconnectedAsync(exception);
    }
}
