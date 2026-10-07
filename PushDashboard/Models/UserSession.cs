namespace PushDashboard.Models;

public class UserSession
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int? DeviceId { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public DateTime FirstLoginAt { get; set; } = DateTime.Now;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime LastActivityAt { get; set; } = DateTime.Now;

    public DateTime? EndedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ApplicationUser User { get; set; } = null!;

    public virtual UserDevice? Device { get; set; }
}
