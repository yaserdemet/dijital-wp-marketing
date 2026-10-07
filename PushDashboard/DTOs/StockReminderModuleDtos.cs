using System.ComponentModel.DataAnnotations;

namespace PushDashboard.DTOs;

/// <summary>
/// Stok hatırlatma modülü ayarları
/// </summary>
public class StockReminderSettingsDto
{
    [Display(Name = "Modül Etkin")]
    public bool Enabled { get; set; }

    [Display(Name = "Email Bildirimi")]
    public bool EmailEnabled { get; set; }

    [Display(Name = "SMS Bildirimi")]
    public bool SmsEnabled { get; set; }

    [Display(Name = "WhatsApp Bildirimi")]
    public bool WhatsAppEnabled { get; set; }

    [Display(Name = "Push Bildirimi")]
    public bool PushEnabled { get; set; }

    [Display(Name = "Stok Eşiği")]
    [Range(1, 10000, ErrorMessage = "Stok eşiği 1-10000 arasında olmalıdır")]
    public int StockThreshold { get; set; } = 1;

    [Display(Name = "Otomatik Stok Bildirimi")]
    public bool AutoNotifyOnStockArrival { get; set; } = true;

    [Display(Name = "Düşük Stok Bildirim Mesajı")]
    public string NotificationMessage { get; set; } = "Merhaba {CustomerName}, {ProductName} ürününün stoğu azalıyor! Son {StockCount} adet kaldı. Hemen sipariş verin!";

    [Display(Name = "Stok Geldi Bildirim Mesajı")]
    public string StockArrivedMessage { get; set; } = "Merhaba {CustomerName}, beklediğiniz {ProductName} ürünü stoklara girdi! Hemen sipariş verin: {ProductUrl}";

    public string? EmailTemplate { get; set; }
    public string? EmailTemplateName { get; set; }
    public string? SmsTemplate { get; set; }
    public string? SmsTemplateName { get; set; }
    public string? WhatsappTemplate { get; set; }
    public string? WhatsappTemplateName { get; set; }

    public List<ActiveChannelDto> AvailableChannels { get; set; } = new();
    public bool HasActiveEcommerceIntegration { get; set; }
    public string? ActiveEcommercePlatform { get; set; }
}

/// <summary>
/// Stok hatırlatma modülü ayarları güncelleme request'i
/// </summary>
public class UpdateStockReminderSettingsRequest
{
    [Required]
    public bool Enabled { get; set; }

    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool WhatsAppEnabled { get; set; }
    public bool PushEnabled { get; set; }

    [Range(1, 10000, ErrorMessage = "Stok eşiği 1-10000 arasında olmalıdır (minimum 1)")]
    public int StockThreshold { get; set; } = 1;

    public bool AutoNotifyOnStockArrival { get; set; } = true;

    [MaxLength(500, ErrorMessage = "Bildirim mesajı en fazla 500 karakter olmalıdır")]
    public string NotificationMessage { get; set; } = "Merhaba {CustomerName}, {ProductName} ürününün stoğu azalıyor! Son {StockCount} adet kaldı. Hemen sipariş verin!";

    [MaxLength(500, ErrorMessage = "Stok geldi mesajı en fazla 500 karakter olmalıdır")]
    public string StockArrivedMessage { get; set; } = "Merhaba {CustomerName}, beklediğiniz {ProductName} ürünü stoklara girdi! Hemen sipariş verin: {ProductUrl}";

    public string? EmailTemplate { get; set; }
    public string? EmailTemplateName { get; set; }
    public string? SmsTemplate { get; set; }
    public string? SmsTemplateName { get; set; }
    public string? WhatsAppTemplate { get; set; }
    public string? WhatsAppTemplateName { get; set; }
}

/// <summary>
/// Stok hatırlatma modülü istatistikleri
/// </summary>
public class StockReminderStatsDto
{
    public int TotalStockReminderNotifications { get; set; }
    public int TotalSubscribers { get; set; }
    public int TotalPendingSubscribers { get; set; }
    public decimal TotalCostThisMonth { get; set; }
    public int NotificationsThisMonth { get; set; }
    public Dictionary<string, int> ChannelUsageStats { get; set; } = new();
    public DateTime? LastNotificationSent { get; set; }
}

/// <summary>
/// Düşük stok bildirimi gönderme sonucu
/// </summary>
public class StockReminderNotificationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int NotificationsSent { get; set; }
    public decimal TotalCost { get; set; }
    public string? ProductName { get; set; }
    public int CurrentStock { get; set; }
    public DateTime SentAt { get; set; } = DateTime.Now;
}
