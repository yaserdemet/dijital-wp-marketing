using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Doğum günü bildirimi gönderim logları
/// </summary>
public class BirthdayNotification
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public int CustomerId { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// Gönderim yapılan doğum günü yılı (aynı yıl içinde tekrar gönderilmemesi için)
    /// </summary>
    public int BirthYear { get; set; }

    /// <summary>
    /// İletişim kanalı (Email, SMS, WhatsApp)
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Kullanılan şablon adı
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Gönderim başarılı mı?
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Hata mesajı (başarısız ise)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gönderim maliyeti
    /// </summary>
    public decimal Cost { get; set; } = 0;

    /// <summary>
    /// Gönderim tarihi
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gönderen kullanıcı ID (manuel gönderim ise)
    /// </summary>
    public string? SentByUserId { get; set; }

    public virtual ApplicationUser? SentByUser { get; set; }

    /// <summary>
    /// Otomatik gönderim mi?
    /// </summary>
    public bool IsAutomatic { get; set; } = false;
}
