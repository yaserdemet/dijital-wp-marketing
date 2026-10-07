using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// WhatsApp mesajlarının takibi için model
/// </summary>
public class WhatsAppMessage
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gupshup'tan dönen mesaj ID'si (gsId)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string GupshupMessageId { get; set; } = string.Empty;

    /// <summary>
    /// Mesajı gönderen şirket
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// Kullanılan WhatsApp entegrasyonu
    /// </summary>
    public int? CompanyIntegrationId { get; set; }

    public virtual CompanyIntegration? CompanyIntegration { get; set; }

    /// <summary>
    /// Alıcı telefon numarası (formatlanmış - örn: 905321234567)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string RecipientPhone { get; set; } = string.Empty;

    /// <summary>
    /// Kullanılan template adı (varsa)
    /// </summary>
    [MaxLength(255)]
    public string? TemplateName { get; set; }

    /// <summary>
    /// Template ID (Gupshup'taki template ID)
    /// </summary>
    [MaxLength(255)]
    public string? TemplateId { get; set; }

    /// <summary>
    /// Mesaj türü: template, text, media vb.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string MessageType { get; set; } = "template";

    /// <summary>
    /// Mesaj içeriği (template değilse, text mesaj ise)
    /// </summary>
    public string? MessageContent { get; set; }

    /// <summary>
    /// Mevcut mesaj durumu: pending, sent, delivered, read, failed
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Hata mesajı (failed durumunda)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Hata kodu (failed durumunda)
    /// </summary>
    [MaxLength(100)]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Mesajın kaynağı: bulk_messaging, order_notification, basket_reminder vb.
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// Kaynak referans ID'si (örn: BulkMessageId, OrderId vb.)
    /// </summary>
    [MaxLength(255)]
    public string? SourceReferenceId { get; set; }

    /// <summary>
    /// Gönderilme zamanı
    /// </summary>
    [Required]
    public DateTime SentAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Teslim edilme zamanı (delivered status'e ulaştığında)
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Okunma zamanı (read status'e ulaştığında)
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Başarısız olma zamanı (failed status'e ulaştığında)
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Son güncelleme zamanı
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Kullanılan WhatsApp App ID
    /// </summary>
    [MaxLength(255)]
    public string? AppId { get; set; }

    /// <summary>
    /// Navigation property - bu mesaj için alınan webhook eventleri
    /// </summary>
    public virtual ICollection<WhatsAppWebhookEvent> WebhookEvents { get; set; } = new List<WhatsAppWebhookEvent>();
}
