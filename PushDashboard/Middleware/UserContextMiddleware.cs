using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Logging;
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
        IUserCacheService userCacheService, ApplicationDbContext dbContext, ISessionService sessionService)
    {
        // Generate correlation ID for request tracking
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Add correlation ID to response headers
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        string? companyName = null;
        string? userId = null;
        Guid? companyId = null;

        try
        {
            // Clear any existing context
            userContextService.Clear();

            // Only process authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    // Try to get CompanyId from claims first (faster)
                    var companyIdClaim = context.User.FindFirst("CompanyId")?.Value;

                    if (!string.IsNullOrEmpty(companyIdClaim) && Guid.TryParse(companyIdClaim, out var parsedCompanyId))
                    {
                        companyId = parsedCompanyId;
                    }

                    // Get user from cache (or database if cache miss)
                    ApplicationUser? user = null;
                    ApplicationUser? userContextUser = null;
                    try
                    {
                        user = await userCacheService.GetUserAsync(userId, dbContext);
                        userContextUser = user;

                        // If we don't have companyId from claims, get it from user
                        if (companyId == null && user?.CompanyId != null)
                        {
                            companyId = user.CompanyId;
                        }

                        // SuperAdmin: override CompanyId from session if available
                        if (user?.IsSuperAdmin == true)
                        {
                            var selectedCompanyId = context.Session.GetString("SuperAdmin_SelectedCompanyId");
                            if (!string.IsNullOrEmpty(selectedCompanyId) && Guid.TryParse(selectedCompanyId, out var superAdminCompanyId))
                            {
                                companyId = superAdminCompanyId;
                                var selectedCompany = await dbContext.Companies
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(c => c.Id == superAdminCompanyId);

                                if (selectedCompany != null)
                                {
                                    userContextUser = CloneUserForContext(user);
                                    userContextUser.CompanyId = selectedCompany.Id;
                                    userContextUser.Company = selectedCompany;
                                    companyName = selectedCompany.Name;
                                }
                                else
                                {
                                    companyName = context.Session.GetString("SuperAdmin_SelectedCompanyName");
                                }
                            }
                        }

                        // Get company name for logging (if not already set by SuperAdmin override)
                        if (companyName == null)
                        {
                            companyName = userContextUser?.Company?.Name ?? user?.Company?.Name;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load user {UserId} in UserContextMiddleware", userId);
                        // Continue without user object, we still have userId and companyId from claims
                    }

                    // Set the context
                    userContextService.SetUserContext(userId, companyId, userContextUser ?? user);

                    try
                    {
                        await sessionService.UpdateDeviceHeartbeatAsync(userId, context);
                    }
                    catch (Exception heartbeatEx)
                    {
                        _logger.LogWarning(heartbeatEx, "Failed to update device heartbeat for user {UserId}", userId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UserContextMiddleware");
        }

        // Set AsyncLocal logging context
        LoggingContext.Set(
            correlationId,
            companyName,
            userId,
            companyId?.ToString(),
            context.Request.Path.Value,
            context.Request.Method
        );

        try
        {
            await _next(context);
        }
        finally
        {
            // Clear AsyncLocal context to prevent memory leaks
            LoggingContext.Clear();
        }
    }

    private static ApplicationUser? CloneUserForContext(ApplicationUser? user)
    {
        if (user == null)
        {
            return null;
        }

        return new ApplicationUser
        {
            Id = user.Id,
            UserName = user.UserName,
            NormalizedUserName = user.NormalizedUserName,
            Email = user.Email,
            NormalizedEmail = user.NormalizedEmail,
            EmailConfirmed = user.EmailConfirmed,
            PasswordHash = user.PasswordHash,
            SecurityStamp = user.SecurityStamp,
            ConcurrencyStamp = user.ConcurrencyStamp,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnd = user.LockoutEnd,
            LockoutEnabled = user.LockoutEnabled,
            AccessFailedCount = user.AccessFailedCount,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CompanyId = user.CompanyId,
            Company = user.Company,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            TwoFactorSecretKey = user.TwoFactorSecretKey,
            CreditBalance = user.CreditBalance,
            ReferralCode = user.ReferralCode,
            IsSuperAdmin = user.IsSuperAdmin,
            ReferralCount = user.ReferralCount,
            ReferredByReferralCode = user.ReferredByReferralCode,
            NotificationPreferences = user.NotificationPreferences,
            UserSessions = user.UserSessions
        };
    }
}
