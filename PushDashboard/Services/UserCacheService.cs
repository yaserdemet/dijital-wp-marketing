using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PushDashboard.Data;
using PushDashboard.Models;

namespace PushDashboard.Services;

/// <summary>
/// Implementation of user caching using IMemoryCache
/// </summary>
public class UserCacheService : IUserCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserCacheService> _logger;
    private static readonly SemaphoreSlim _cacheLock = new(1, 1);
    
    // Cache configuration
    private const int CacheDurationMinutes = 30;
    private const string CacheKeyPrefix = "user:";
    private const string CompanyCacheKeyPrefix = "company_users:";

    public UserCacheService(IMemoryCache cache, ILogger<UserCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApplicationUser?> GetUserAsync(string userId, ApplicationDbContext dbContext)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var cacheKey = GetUserCacheKey(userId);

        // Try to get from cache first
        if (_cache.TryGetValue<ApplicationUser>(cacheKey, out var cachedUser))
        {
            _logger.LogDebug("User {UserId} retrieved from cache", userId);
            return cachedUser;
        }

        // Cache miss - acquire lock to prevent multiple simultaneous database queries
        await _cacheLock.WaitAsync();
        try
        {
            // Double-check cache after acquiring lock (another thread might have populated it)
            if (_cache.TryGetValue<ApplicationUser>(cacheKey, out cachedUser))
            {
                _logger.LogDebug("User {UserId} retrieved from cache (after lock)", userId);
                return cachedUser;
            }

            // Query database
            _logger.LogDebug("Cache miss for user {UserId}, querying database", userId);
            var user = await dbContext.Users
                .Include(u => u.Company)
                .AsNoTracking() // Important: don't track entities in cache
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                // Cache the user with sliding expiration
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(CacheDurationMinutes))
                    .SetPriority(CacheItemPriority.Normal);

                _cache.Set(cacheKey, user, cacheOptions);

                // Also track this user under their company for bulk invalidation
                if (user.CompanyId.HasValue)
                {
                    TrackUserInCompany(user.CompanyId.Value, userId);
                }

                _logger.LogDebug("User {UserId} cached for {Minutes} minutes", userId, CacheDurationMinutes);
            }

            return user;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public Task InvalidateUserAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        var cacheKey = GetUserCacheKey(userId);
        _cache.Remove(cacheKey);
        _logger.LogInformation("Cache invalidated for user {UserId}", userId);
        
        return Task.CompletedTask;
    }

    public Task InvalidateAllUsersAsync(Guid companyId)
    {
        var companyCacheKey = GetCompanyCacheKey(companyId);
        
        // Get list of users for this company
        if (_cache.TryGetValue<HashSet<string>>(companyCacheKey, out var userIds))
        {
            foreach (var userId in userIds)
            {
                var userCacheKey = GetUserCacheKey(userId);
                _cache.Remove(userCacheKey);
            }
            
            _cache.Remove(companyCacheKey);
            _logger.LogInformation("Cache invalidated for {Count} users in company {CompanyId}", userIds.Count, companyId);
        }
        
        return Task.CompletedTask;
    }

    private void TrackUserInCompany(Guid companyId, string userId)
    {
        var companyCacheKey = GetCompanyCacheKey(companyId);
        
        if (!_cache.TryGetValue<HashSet<string>>(companyCacheKey, out var userIds))
        {
            userIds = new HashSet<string>();
        }

        userIds.Add(userId);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(CacheDurationMinutes))
            .SetPriority(CacheItemPriority.Low);

        _cache.Set(companyCacheKey, userIds, cacheOptions);
    }

    private static string GetUserCacheKey(string userId) => $"{CacheKeyPrefix}{userId}";
    private static string GetCompanyCacheKey(Guid companyId) => $"{CompanyCacheKeyPrefix}{companyId}";
}
