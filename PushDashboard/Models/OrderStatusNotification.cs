using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Sipariş durumu değişikliği bildirim ayarları
/// </summary>
public class OrderStatusNotification
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// Sipariş durumu (Ticimax'tan gelen durum kodu)
    /// </summary>
    public string OrderStatus { get; set; } = string.Empty;

    /// <summary>
    /// Sipariş durumunun görüntülenen adı
    /// </summary>
    public string OrderStatusDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Bu durum için bildirim aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// E-posta bildirimi aktif mi?
    /// </summary>
    public bool EmailNotificationEnabled { get; set; } = false;

    /// <summary>
    /// E-posta şablonu ID'si
    /// </summary>
    public int? EmailTemplateId { get; set; }

    /// <summary>
    /// SMS bildirimi aktif mi?
    /// </summary>
    public bool SmsNotificationEnabled { get; set; } = false;

    /// <summary>
    /// SMS şablonu ID'si
    /// </summary>
    public int? SmsTemplateId { get; set; }

    /// <summary>
    /// WhatsApp bildirimi aktif mi?
    /// </summary>
    public bool WhatsAppNotificationEnabled { get; set; } = false;

    /// <summary>
    /// WhatsApp şablonu ID'si
    /// </summary>
    public string? WhatsAppTemplateId { get; set; }

    /// <summary>
    /// Bildirim gönderilme sırası (1: hemen, 2: 1 saat sonra, vb.)
    /// </summary>
    public int NotificationOrder { get; set; } = 1;

    /// <summary>
    /// Bildirim gecikmesi (dakika cinsinden)
    /// </summary>
    public int DelayMinutes { get; set; } = 0;

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Güncellenme tarihi
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Son bildirim gönderilme tarihi
    /// </summary>
    public DateTime? LastNotificationAt { get; set; }

    /// <summary>
    /// Toplam gönderilen bildirim sayısı
    /// </summary>
    public int TotalNotificationsSent { get; set; } = 0;

    /// <summary>
    /// Ek ayarlar (JSON formatında)
    /// </summary>
    public string? AdditionalSettings { get; set; }
}

/// <summary>
/// Sipariş durumu değişikliği geçmişi
/// </summary>
public class OrderStatusChangeLog
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// Sipariş ID'si (e-ticaret platformundan gelen)
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Sipariş numarası
    /// </summary>
    public string? OrderNumber { get; set; }

    /// <summary>
    /// Müşteri e-posta adresi
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;

    public int? CustomerId { get; set; }

    /// <summary>
    /// Müşteri adı
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Müşteri telefonu
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Eski sipariş durumu
    /// </summary>
    public string? OldStatus { get; set; }

    /// <summary>
    /// Yeni sipariş durumu
    /// </summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>
    /// Yeni durumun görüntülenen adı
    /// </summary>
    public string? NewStatusDisplayName { get; set; }

    /// <summary>
    /// Sipariş tutarı
    /// </summary>
    public decimal? OrderAmount { get; set; }

    /// <summary>
    /// Sipariş para birimi
    /// </summary>
    public string? OrderCurrency { get; set; }

    /// <summary>
    /// Durum değişikliği tarihi
    /// </summary>
    public DateTime StatusChangedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Webhook'tan gelen ham veri (JSON)
    /// </summary>
    public string? WebhookPayload { get; set; }

    /// <summary>
    /// Bildirim gönderildi mi?
    /// </summary>
    public bool NotificationSent { get; set; } = false;

    /// <summary>
    /// Bildirim gönderilme tarihi
    /// </summary>
    public DateTime? NotificationSentAt { get; set; }

    /// <summary>
    /// Bildirim kanalları (JSON: ["email", "sms", "whatsapp"])
    /// </summary>
    public string? NotificationChannels { get; set; }

    /// <summary>
    /// Bildirim hatası varsa hata mesajı
    /// </summary>
    public string? NotificationError { get; set; }

    /// <summary>
    /// Kayıt oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
