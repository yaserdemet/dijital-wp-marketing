using PushDashboard.Data;
using PushDashboard.Models;

namespace PushDashboard.Services;

/// <summary>
/// Service for caching user data to avoid repeated database queries
/// </summary>
public interface IUserCacheService
{
    /// <summary>
    /// Gets a user from cache or database. Automatically caches the result.
    /// </summary>
    /// <param name="userId">The user ID to retrieve</param>
    /// <param name="dbContext">Database context for fallback queries</param>
    /// <returns>The user with company data, or null if not found</returns>
    Task<ApplicationUser?> GetUserAsync(string userId, ApplicationDbContext dbContext);

    /// <summary>
    /// Invalidates the cache for a specific user
    /// </summary>
    /// <param name="userId">The user ID to invalidate</param>
    Task InvalidateUserAsync(string userId);

    /// <summary>
    /// Invalidates the cache for all users in a company
    /// </summary>
    /// <param name="companyId">The company ID whose users should be invalidated</param>
    Task InvalidateAllUsersAsync(Guid companyId);
}
