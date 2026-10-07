using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services;
using System.Security.Claims;

namespace PushDashboard.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserContextService userContextService, 
        ApplicationDbContext dbContext)
    {
        try
        {
            // Clear any existing context
            userContextService.Clear();

            // Only process authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    // Try to get CompanyId from claims first (faster)
                    var companyIdClaim = context.User.FindFirst("CompanyId")?.Value;
                    Guid? companyId = null;
                    
                    if (!string.IsNullOrEmpty(companyIdClaim) && Guid.TryParse(companyIdClaim, out var parsedCompanyId))
                    {
                        companyId = parsedCompanyId;
                    }

                    // Get user from database (we'll cache this in future if needed)
                    ApplicationUser? user = null;
                    try
                    {
                        user = await dbContext.Users
                            .Include(u => u.Company)
                            .FirstOrDefaultAsync(u => u.Id == userId);

                        // If we don't have companyId from claims, get it from user
                        if (companyId == null && user?.CompanyId != null)
                        {
                            companyId = user.CompanyId;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load user {UserId} from database in UserContextMiddleware", userId);
                        // Continue without user object, we still have userId and companyId from claims
                    }

                    // Set the context
                    userContextService.SetUserContext(userId, companyId, user);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UserContextMiddleware");
            // Don't fail the request, just continue without context
        }

        await _next(context);
    }
}
