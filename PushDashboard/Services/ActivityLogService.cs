using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;

namespace PushDashboard.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(ApplicationDbContext context, ILogger<ActivityLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActivityAsync(
        string userId,
        Guid companyId,
        string actionType,
        string module,
        string description,
        string? entityId = null,
        string? entityType = null,
        object? details = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var activityLog = new ActivityLog
            {
                UserId = userId,
                CompanyId = companyId,
                ActionType = actionType,
                Module = module,
                Description = description,
                EntityId = entityId,
                EntityType = entityType,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                IpAddress = ipAddress?.Length > 50 ? ipAddress.Substring(0, 50) : ipAddress,
                UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent,
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktivite logu kaydedilemedi: {Description}", description);
            // Log hatası kritik değil, uygulamanın devam etmesine izin ver
        }
    }

    public async Task<(List<ActivityLogViewModel> Logs, int TotalCount)> GetActivityLogsAsync(
        Guid companyId,
        int page = 1,
        int pageSize = 20,
        string? module = null,
        string? actionType = null,
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchTerm = null)
    {
        var query = _context.ActivityLogs
            .Include(a => a.User)
            .Where(a => a.CompanyId == companyId)
            .AsQueryable();

        // Filtreleme
        if (!string.IsNullOrEmpty(module))
            query = query.Where(a => a.Module == module);

        if (!string.IsNullOrEmpty(actionType))
            query = query.Where(a => a.ActionType == actionType);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(a => a.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(a => a.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.CreatedAt <= endDate.Value.AddDays(1));

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(a => a.Description.Contains(searchTerm) ||
                                     (a.User != null && (a.User.FirstName + " " + a.User.LastName).Contains(searchTerm)));

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ActivityLogViewModel
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? (a.User.FirstName + " " + a.User.LastName).Trim() : "Bilinmeyen",
                UserEmail = a.User != null ? a.User.Email ?? "" : "",
                ActionType = a.ActionType,
                ActionTypeDisplay = GetActionTypeDisplay(a.ActionType),
                Module = a.Module,
                Description = a.Description,
                Details = a.Details,
                EntityId = a.EntityId,
                EntityType = a.EntityType,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt,
                TimeAgo = GetTimeAgo(a.CreatedAt),
                ActionIcon = GetActionIcon(a.ActionType),
                ActionColor = GetActionColor(a.ActionType)
            })
            .ToListAsync();

        foreach (var log in logs)
        {
            log.IpAddress = FormatIpAddress(log.IpAddress);
        }

        return (logs, totalCount);
    }

    public async Task<List<ActivityLogViewModel>> GetUserActivityLogsAsync(string userId, int limit = 50)
    {
        var logs = await _context.ActivityLogs
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new ActivityLogViewModel
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? (a.User.FirstName + " " + a.User.LastName).Trim() : "Bilinmeyen",
                UserEmail = a.User != null ? a.User.Email ?? "" : "",
                ActionType = a.ActionType,
                ActionTypeDisplay = GetActionTypeDisplay(a.ActionType),
                Module = a.Module,
                Description = a.Description,
                Details = a.Details,
                EntityId = a.EntityId,
                EntityType = a.EntityType,
                IpAddress = a.IpAddress,
                CreatedAt = a.CreatedAt,
                TimeAgo = GetTimeAgo(a.CreatedAt),
                ActionIcon = GetActionIcon(a.ActionType),
                ActionColor = GetActionColor(a.ActionType)
            })
            .ToListAsync();

        foreach (var log in logs)
        {
            log.IpAddress = FormatIpAddress(log.IpAddress);
        }

        return logs;
    }

    public async Task<ActivityLogStatsViewModel> GetActivityStatsAsync(Guid companyId, int lastDays = 30)
    {
        var startDate = DateTime.Now.AddDays(-lastDays);
        var today = DateTime.Now.Date;

        var query = _context.ActivityLogs
            .Where(a => a.CompanyId == companyId && a.CreatedAt >= startDate);

        var totalActions = await query.CountAsync();
        var todayActions = await query.CountAsync(a => a.CreatedAt >= today);
        var uniqueUsers = await query.Select(a => a.UserId).Distinct().CountAsync();

        var actionsByType = await query
            .GroupBy(a => a.ActionType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        var actionsByModule = await query
            .GroupBy(a => a.Module)
            .Select(g => new { Module = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Module, x => x.Count);

        var dailyActivity = await query
            .GroupBy(a => a.CreatedAt.Date)
            .Select(g => new DailyActivityViewModel
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        return new ActivityLogStatsViewModel
        {
            TotalActions = totalActions,
            TodayActions = todayActions,
            UniqueUsers = uniqueUsers,
            ActionsByType = actionsByType,
            ActionsByModule = actionsByModule,
            DailyActivity = dailyActivity
        };
    }

    private static string GetActionTypeDisplay(string actionType)
    {
        return actionType switch
        {
            ActivityActionTypes.Create => "Oluşturma",
            ActivityActionTypes.Update => "Güncelleme",
            ActivityActionTypes.Delete => "Silme",
            ActivityActionTypes.View => "Görüntüleme",
            ActivityActionTypes.Login => "Giriş",
            ActivityActionTypes.Logout => "Çıkış",
            ActivityActionTypes.Export => "Dışa Aktarma",
            ActivityActionTypes.Import => "İçe Aktarma",
            ActivityActionTypes.Sync => "Senkronizasyon",
            ActivityActionTypes.Send => "Gönderim",
            ActivityActionTypes.Activate => "Aktifleştirme",
            ActivityActionTypes.Deactivate => "Pasifleştirme",
            ActivityActionTypes.Settings => "Ayar Değişikliği",
            ActivityActionTypes.Purchase => "Satın Alma",
            ActivityActionTypes.Configure => "Yapılandırma",
            _ => actionType
        };
    }

    private static string GetActionIcon(string actionType)
    {
        return actionType switch
        {
            ActivityActionTypes.Create => "fas fa-plus-circle",
            ActivityActionTypes.Update => "fas fa-edit",
            ActivityActionTypes.Delete => "fas fa-trash",
            ActivityActionTypes.View => "fas fa-eye",
            ActivityActionTypes.Login => "fas fa-sign-in-alt",
            ActivityActionTypes.Logout => "fas fa-sign-out-alt",
            ActivityActionTypes.Export => "fas fa-file-export",
            ActivityActionTypes.Import => "fas fa-file-import",
            ActivityActionTypes.Sync => "fas fa-sync",
            ActivityActionTypes.Send => "fas fa-paper-plane",
            ActivityActionTypes.Activate => "fas fa-toggle-on",
            ActivityActionTypes.Deactivate => "fas fa-toggle-off",
            ActivityActionTypes.Settings => "fas fa-cog",
            ActivityActionTypes.Purchase => "fas fa-shopping-cart",
            ActivityActionTypes.Configure => "fas fa-wrench",
            _ => "fas fa-info-circle"
        };
    }

    private static string GetActionColor(string actionType)
    {
        return actionType switch
        {
            ActivityActionTypes.Create => "text-green-600 bg-green-100",
            ActivityActionTypes.Update => "text-blue-600 bg-blue-100",
            ActivityActionTypes.Delete => "text-red-600 bg-red-100",
            ActivityActionTypes.View => "text-gray-600 bg-gray-100",
            ActivityActionTypes.Login => "text-indigo-600 bg-indigo-100",
            ActivityActionTypes.Logout => "text-orange-600 bg-orange-100",
            ActivityActionTypes.Export => "text-purple-600 bg-purple-100",
            ActivityActionTypes.Import => "text-cyan-600 bg-cyan-100",
            ActivityActionTypes.Sync => "text-teal-600 bg-teal-100",
            ActivityActionTypes.Send => "text-pink-600 bg-pink-100",
            ActivityActionTypes.Activate => "text-emerald-600 bg-emerald-100",
            ActivityActionTypes.Deactivate => "text-amber-600 bg-amber-100",
            ActivityActionTypes.Settings => "text-slate-600 bg-slate-100",
            ActivityActionTypes.Purchase => "text-lime-600 bg-lime-100",
            ActivityActionTypes.Configure => "text-violet-600 bg-violet-100",
            _ => "text-gray-600 bg-gray-100"
        };
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "Az önce";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} dakika önce";
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

    private static string? FormatIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        var delimiterChars = new[] { ',', '|', ' ', '`' };
        var parts = ipAddress.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (parts.Count == 0)
            return null;

        var ipv4Addresses = new List<string>();
        var hasLocalhost = false;

        foreach (var part in parts)
        {
            var cleaned = part;

            if (cleaned.StartsWith(":ffff:", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(6);
            }
            else if (cleaned.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7);
            }

            if (cleaned == "::1")
            {
                hasLocalhost = true;
                continue;
            }

            if (IsIpv4Address(cleaned))
            {
                ipv4Addresses.Add(cleaned);
            }
        }

        if (ipv4Addresses.Count > 0)
        {
            return ipv4Addresses.First();
        }

        if (hasLocalhost)
        {
            return "Local";
        }

        // Truncate to fit database column max length (50)
        if (ipAddress.Length > 50)
            return ipAddress.Substring(0, 50);

        return ipAddress;
    }

    private static bool IsIpv4Address(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;

        var parts = ip.Split('.');
        if (parts.Length != 4)
            return false;

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var num) || num < 0 || num > 255)
                return false;
        }

        return true;
    }
}
