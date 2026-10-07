namespace PushDashboard.Models;

public class UserDevice
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string DeviceFingerprint { get; set; } = string.Empty;

    public string? DeviceId { get; set; }

    public string? OperatingSystem { get; set; }

    public string? Browser { get; set; }

    public string? BrowserFamily { get; set; }

    public string? DeviceType { get; set; }

    public string? LastIpAddress { get; set; }

    public string? LastLocation { get; set; }

    public DateTime FirstSeenAt { get; set; } = DateTime.Now;

    public DateTime LastActivityAt { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    public bool IsCurrent { get; set; } = false;

    public bool IsLocalhost { get; set; } = false;

    public bool IsIncognito { get; set; } = false;

    public virtual ApplicationUser User { get; set; } = null!;

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
