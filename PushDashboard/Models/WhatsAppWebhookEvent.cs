using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Gupshup'tan gelen webhook eventlerini saklamak için model
/// Debug ve audit amaçlı raw event kayıtları
/// </summary>
public class WhatsAppWebhookEvent
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// İlişkili şirket
    /// </summary>
    [Required]
    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// İlişkili WhatsApp mesajı (eğer eşleştirilebildiyse)
    /// </summary>
    public int? WhatsAppMessageId { get; set; }

    public virtual WhatsAppMessage? WhatsAppMessage { get; set; }

    /// <summary>
    /// Event türü (Gupshup'tan gelen - örn: message-event, user-event vb.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event alt türü (örn: enqueued, sent, delivered, read, failed)
    /// </summary>
    [MaxLength(100)]
    public string? EventSubType { get; set; }

    /// <summary>
    /// Gupshup mesaj ID'si (gsId)
    /// </summary>
    [MaxLength(255)]
    public string? GupshupMessageId { get; set; }

    /// <summary>
    /// Ham JSON payload (debugging için)
    /// </summary>
    [Required]
    [Column(TypeName = "text")]
    public string RawPayload { get; set; } = string.Empty;

    /// <summary>
    /// Event işlendi mi?
    /// </summary>
    [Required]
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// İşlenme zamanı
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// İşlenirken hata oluştu mu?
    /// </summary>
    public string? ProcessingError { get; set; }

    /// <summary>
    /// Webhook'un alınma zamanı
    /// </summary>
    [Required]
    public DateTime ReceivedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Event'in Gupshup'taki timestamp'i (milliseconds)
    /// </summary>
    public long? GupshupTimestamp { get; set; }

    /// <summary>
    /// Webhook verification token doğru muydu?
    /// </summary>
    [Required]
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Webhook request'ten gelen IP adresi
    /// </summary>
    [MaxLength(50)]
    public string? SourceIp { get; set; }
}
