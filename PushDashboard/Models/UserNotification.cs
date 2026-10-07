namespace PushDashboard.Models;

/// <summary>
/// Kullanıcı bildirimi için model
/// </summary>
public class UserNotification
{
    public int Id { get; set; }
    
    /// <summary>
    /// Bildirimin sahibi kullanıcı
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;
    
    /// <summary>
    /// İlgili şirket (opsiyonel)
    /// </summary>
    public Guid? CompanyId { get; set; }
    public virtual Company? Company { get; set; }
    
    /// <summary>
    /// Bildirim başlığı
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Bildirim mesajı
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Bildirim tipi (info, success, warning, error)
    /// </summary>
    public NotificationType Type { get; set; } = NotificationType.Info;
    
    /// <summary>
    /// FontAwesome icon sınıfı (örn: "fas fa-bell")
    /// </summary>
    public string Icon { get; set; } = "fas fa-bell";
    
    /// <summary>
    /// Icon renk sınıfı (örn: "text-blue-500")
    /// </summary>
    public string IconColor { get; set; } = "text-blue-500";
    
    /// <summary>
    /// Icon arka plan sınıfı (örn: "bg-blue-100")
    /// </summary>
    public string IconBackground { get; set; } = "bg-blue-100";
    
    /// <summary>
    /// Okundu durumu
    /// </summary>
    public bool IsRead { get; set; } = false;
    
    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Okunma tarihi
    /// </summary>
    public DateTime? ReadAt { get; set; }
    
    /// <summary>
    /// Tıklandığında yönlendirilecek URL (opsiyonel)
    /// </summary>
    public string? ActionUrl { get; set; }
}

/// <summary>
/// Bildirim tipleri
/// </summary>
public enum NotificationType
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}
