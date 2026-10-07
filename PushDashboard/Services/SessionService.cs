using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using PushDashboard.Hubs;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using UAParser;
using System.Security.Cryptography;
using System.Text;

namespace PushDashboard.Services;

public interface ISessionService
{
    Task<UserSession> CreateSessionAsync(string userId, string sessionId, HttpContext httpContext);
    Task<UserDevice> GetOrCreateDeviceAsync(string userId, HttpContext httpContext);
    Task UpdateDeviceHeartbeatAsync(string userId, HttpContext httpContext);
    Task<List<ActiveDeviceViewModel>> GetActiveDevicesAsync(string userId, int? currentDeviceId = null, bool includeLocalhost = false, bool includeIncognito = false);
    Task<bool> TerminateDeviceAsync(string userId, int deviceId);
    Task<int> TerminateAllOtherDevicesAsync(string userId, int currentDeviceId);
    string GetDeviceFingerprint(string userId, string os, string browserFamily, string? deviceId);
}

public class SessionService : ISessionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionService> _logger;
    private readonly IHubContext<SessionHub> _hubContext;
    private readonly IActivityLogService? _activityLogService;

    public SessionService(ApplicationDbContext context, ILogger<SessionService> logger, IHubContext<SessionHub> hubContext, IActivityLogService? activityLogService = null)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
        _activityLogService = activityLogService;
    }

    public async Task<UserSession> CreateSessionAsync(string userId, string sessionId, HttpContext httpContext)
    {
        try
        {
            var device = await GetOrCreateDeviceAsync(userId, httpContext);

            await _context.UserDevices
                .Where(d => d.UserId == userId && d.IsActive)
                .ExecuteUpdateAsync(d => d.SetProperty(x => x.IsCurrent, false));

            device.IsCurrent = true;

            var now = DateTime.Now;
            var session = new UserSession
            {
                UserId = userId,
                DeviceId = device.Id,
                SessionId = sessionId,
                FirstLoginAt = now,
                CreatedAt = now,
                LastActivityAt = now,
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new session {SessionId} for user {UserId} on device {DeviceId}", sessionId, userId, device.Id);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user {UserId}", userId);
            throw;
        }
    }

    private string GetLocationFromIp(string ipAddress)
    {
        // Bu basit bir implementasyon. Gerçek uygulamada IP geolocation servisi kullanılabilir
        if (ipAddress == "127.0.0.1" || ipAddress == "::1" || ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("10."))
        {
            return "Yerel Ağ";
        }

        // Gerçek uygulamada burada IP geolocation API'si kullanılabilir
        return "İstanbul, Türkiye";
    }

    private string GetClientIpAddress(HttpContext httpContext)
    {
        var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        // X-Forwarded-For can contain multiple IPs: "client, proxy1, proxy2"
        // We only want the first (original client) IP
        if (!string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = ipAddress.Split(',')[0].Trim();
        }

        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        }

        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        }

        // Safety: truncate to 45 chars to match varchar(45) column
        if (ipAddress != null && ipAddress.Length > 45)
        {
            ipAddress = ipAddress[..45];
        }

        return ipAddress ?? "Unknown";
    }

    private string GetDeviceType(ClientInfo clientInfo)
    {
        var deviceFamily = clientInfo.Device.Family.ToLower();

        if (deviceFamily.Contains("mobile") || deviceFamily.Contains("phone"))
        {
            return "Mobile";
        }

        if (deviceFamily.Contains("tablet") || deviceFamily.Contains("ipad"))
        {
            return "Tablet";
        }

        return "Desktop";
    }

    private async Task NotifySessionTerminatedAsync(string userId, int sessionId)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("SessionTerminated", new { sessionId, message = "Oturumunuz başka bir cihazdan sonlandırıldı." });

            _logger.LogInformation("Sent session termination notification to user {UserId} for session {SessionId}", userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending session termination notification to user {UserId}", userId);
        }
    }

    public async Task<UserDevice> GetOrCreateDeviceAsync(string userId, HttpContext httpContext)
    {
        try
        {
            var deviceId = GetOrCreateDeviceId(httpContext);
            var isIncognito = IsIncognitoMode(httpContext);

            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            var parser = Parser.GetDefault();
            var clientInfo = parser.Parse(userAgent);

            var os = $"{clientInfo.OS.Family} {clientInfo.OS.Major}";
            var browserFamily = clientInfo.UA.Family;
            var browser = $"{browserFamily} {clientInfo.UA.Major}";
            var deviceType = GetDeviceType(clientInfo);

            var fingerprint = GetDeviceFingerprint(userId, os, browserFamily, deviceId);

            var device = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceFingerprint == fingerprint && d.UserId == userId);

            var ipAddress = GetClientIpAddress(httpContext);
            var location = GetLocationFromIp(ipAddress);
            var isLocalhost = IsLocalhost(ipAddress);

            if (device == null)
            {
                try
                {
                    device = new UserDevice
                    {
                        UserId = userId,
                        DeviceFingerprint = fingerprint,
                        DeviceId = deviceId,
                        OperatingSystem = os,
                        Browser = browser,
                        BrowserFamily = browserFamily,
                        DeviceType = deviceType,
                        LastIpAddress = ipAddress,
                        LastLocation = location,
                        FirstSeenAt = DateTime.Now,
                        LastActivityAt = DateTime.Now,
                        IsActive = true,
                        IsCurrent = false,
                        IsLocalhost = isLocalhost,
                        IsIncognito = isIncognito
                    };

                    _context.UserDevices.Add(device);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message?.Contains("IX_UserDevices_DeviceFingerprint") == true || 
                                                     dbEx.InnerException?.Message?.Contains("UNIQUE constraint") == true)
                {
                    _logger.LogWarning("Device with fingerprint {Fingerprint} already exists, attempting to retrieve it", fingerprint);
                    
                    device = await _context.UserDevices
                        .FirstOrDefaultAsync(d => d.DeviceFingerprint == fingerprint && d.UserId == userId);
                    
                    if (device == null)
                    {
                        throw new InvalidOperationException($"Device with fingerprint {fingerprint} should exist but was not found after unique constraint violation");
                    }
                    
                    if (!device.IsActive)
                    {
                        device.IsActive = true;
                    }
                    
                    device.LastActivityAt = DateTime.Now;
                    device.LastIpAddress = ipAddress;
                    device.LastLocation = location;
                    device.IsLocalhost = isLocalhost;
                    device.IsIncognito = isIncognito;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Created new device {DeviceId} for user {UserId}", device.Id, userId);

                if (!isLocalhost && !isIncognito && _activityLogService != null)
                {
                    try
                    {
                        var user = await _context.Users
                            .FirstOrDefaultAsync(u => u.Id == userId);

                        if (user?.CompanyId != null)
                        {
                            await _activityLogService.LogActivityAsync(
                                userId,
                                user.CompanyId.Value,
                                ActivityActionTypes.Create,
                                ActivityModules.Account,
                                $"Yeni cihaz eklendi: {browser} - {os} ({deviceType})",
                                entityId: device.Id.ToString(),
                                entityType: "UserDevice",
                                details: new
                                {
                                    DeviceId = device.Id,
                                    Browser = browser,
                                    OperatingSystem = os,
                                    DeviceType = deviceType,
                                    Location = location
                                },
                                ipAddress: ipAddress,
                                userAgent: userAgent
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to log device creation activity for user {UserId}", userId);
                    }
                }
            }
            else
            {
                if (!device.IsActive)
                {
                    device.IsActive = true;
                }

                device.LastActivityAt = DateTime.Now;
                device.LastIpAddress = ipAddress;
                device.LastLocation = location;
                device.IsLocalhost = isLocalhost;
                device.IsIncognito = isIncognito;
                await _context.SaveChangesAsync();
            }

            return device;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating device for user {UserId}", userId);
            throw;
        }
    }

    public string GetDeviceFingerprint(string userId, string os, string browserFamily, string? deviceId)
    {
        var input = $"{userId}_{os}_{browserFamily}_{deviceId ?? "unknown"}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }

    public async Task UpdateDeviceHeartbeatAsync(string userId, HttpContext httpContext)
    {
        try
        {
            var deviceId = GetOrCreateDeviceId(httpContext);
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            var parser = Parser.GetDefault();
            var clientInfo = parser.Parse(userAgent);

            var os = $"{clientInfo.OS.Family} {clientInfo.OS.Major}";
            var browserFamily = clientInfo.UA.Family;
            var fingerprint = GetDeviceFingerprint(userId, os, browserFamily, deviceId);

            var device = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceFingerprint == fingerprint && d.UserId == userId);

            if (device == null)
            {
                return;
            }

            var ipAddress = GetClientIpAddress(httpContext);
            var location = GetLocationFromIp(ipAddress);

            if (device.LastIpAddress != ipAddress)
            {
                device.LastIpAddress = ipAddress;
                device.LastLocation = location;
            }

            device.LastActivityAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device heartbeat for user {UserId}", userId);
        }
    }

    public async Task<List<ActiveDeviceViewModel>> GetActiveDevicesAsync(string userId, int? currentDeviceId = null, bool includeLocalhost = false, bool includeIncognito = false)
    {
        try
        {
            var query = _context.UserDevices
                .Include(d => d.UserSessions)
                .Where(d => d.UserId == userId && d.IsActive);

            if (!includeLocalhost)
            {
                query = query.Where(d => !d.IsLocalhost);
            }

            if (!includeIncognito)
            {
                query = query.Where(d => !d.IsIncognito);
            }

            var devices = await query
                .OrderByDescending(d => d.LastActivityAt)
                .ToListAsync();

            var result = devices.Select(d => new ActiveDeviceViewModel
            {
                Id = d.Id,
                OperatingSystem = d.OperatingSystem ?? "",
                Browser = d.Browser ?? "",
                BrowserFamily = d.BrowserFamily ?? "",
                DeviceType = d.DeviceType ?? "",
                LastIpAddress = d.LastIpAddress ?? "",
                LastLocation = d.LastLocation ?? "",
                FirstSeenAt = d.FirstSeenAt,
                LastActivityAt = d.LastActivityAt,
                IsCurrent = d.IsCurrent || (currentDeviceId.HasValue && d.Id == currentDeviceId.Value),
                SessionCount = d.UserSessions.Count(s => s.IsActive)
            }).ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active devices for user {UserId}", userId);
            return new List<ActiveDeviceViewModel>();
        }
    }

    public async Task<bool> TerminateDeviceAsync(string userId, int deviceId)
    {
        try
        {
            var device = await _context.UserDevices
                .Include(d => d.UserSessions)
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId && d.IsActive);

            if (device == null)
            {
                return false;
            }

            device.IsActive = false;
            device.IsCurrent = false;

            foreach (var session in device.UserSessions.Where(s => s.IsActive))
            {
                session.IsActive = false;
                session.EndedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            foreach (var session in device.UserSessions)
            {
                await NotifySessionTerminatedAsync(userId, session.Id);
            }

            _logger.LogInformation("Terminated device {DeviceId} for user {UserId}", deviceId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating device {DeviceId} for user {UserId}", deviceId, userId);
            return false;
        }
    }

    public async Task<int> TerminateAllOtherDevicesAsync(string userId, int currentDeviceId)
    {
        try
        {
            var devices = await _context.UserDevices
                .Include(d => d.UserSessions)
                .Where(d => d.UserId == userId && d.Id != currentDeviceId && d.IsActive)
                .ToListAsync();

            var terminatedCount = 0;

            foreach (var device in devices)
            {
                device.IsActive = false;
                device.IsCurrent = false;

                foreach (var session in device.UserSessions.Where(s => s.IsActive))
                {
                    session.IsActive = false;
                    session.EndedAt = DateTime.Now;
                    terminatedCount++;
                }
            }

            await _context.SaveChangesAsync();

            foreach (var device in devices)
            {
                foreach (var session in device.UserSessions)
                {
                    await NotifySessionTerminatedAsync(userId, session.Id);
                }
            }

            _logger.LogInformation("Terminated {Count} other devices for user {UserId}", devices.Count, userId);
            return terminatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating other devices for user {UserId}", userId);
            return 0;
        }
    }

    private bool IsIncognitoMode(HttpContext httpContext)
    {
        var deviceIdCookie = httpContext.Request.Cookies["pushonice_device_id"];
        if (string.IsNullOrEmpty(deviceIdCookie))
        {
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString().ToLower();
            return userAgent.Contains("incognito") ||
                   userAgent.Contains("private") ||
                   userAgent.Contains("inprivate");
        }
        return false;
    }

    private string GetOrCreateDeviceId(HttpContext httpContext)
    {
        var deviceIdCookie = httpContext.Request.Cookies["pushonice_device_id"];

        if (!string.IsNullOrEmpty(deviceIdCookie))
        {
            return deviceIdCookie;
        }

        if (IsIncognitoMode(httpContext))
        {
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userAgent + DateTime.Now.Ticks));
            return $"incognito_{Convert.ToBase64String(hashBytes)}";
        }

        var newDeviceId = Guid.NewGuid().ToString();
        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Lax
        };
        httpContext.Response.Cookies.Append("pushonice_device_id", newDeviceId, cookieOptions);

        return newDeviceId;
    }

    private bool IsLocalhost(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;

        return ipAddress == "127.0.0.1" ||
               ipAddress == "::1" ||
               ipAddress.StartsWith("192.168.") ||
               ipAddress.StartsWith("10.") ||
               ipAddress == "localhost";
    }
}
