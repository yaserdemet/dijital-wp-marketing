using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Sepet hatırlatma zamanlaması ayarları
/// </summary>
public class BasketReminderSchedule
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public int ReminderTimeForHours { get; set; } // 6, 24, 168 (1 hafta)

    public string NotificationContent { get; set; } = string.Empty; // Maksimum 300 karakter

    /// <summary>
    /// Seçilen iletişim kanalları (JSON array: ["WhatsApp", "Email", "Telegram"])
    /// </summary>
    public string CommunicationChannels { get; set; } = "[]";

    /// <summary>
    /// Kanal bazında mesaj içerikleri (JSON object: {"WhatsApp": "mesaj", "Email": "mesaj"})
    /// </summary>
    public string ChannelMessages { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public bool AllowResendForSentCustomers { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string CreatedByUserId { get; set; } = string.Empty;

    public virtual ApplicationUser CreatedByUser { get; set; } = null!;

    // Helper properties
    [NotMapped]
    public string ReminderTimeText
    {
        get
        {
            return ReminderTimeForHours switch
            {
                6 => "6 Saat",
                24 => "1 Gün",
                168 => "1 Hafta",
                _ => $"{ReminderTimeForHours} Saat"
            };
        }
    }

    [NotMapped]
    public string StatusText => IsActive ? "Aktif" : "Pasif";

    [NotMapped]
    public string StatusBadgeClass => IsActive ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";
}

/// <summary>
/// Sepet hatırlatma gönderim logları
/// </summary>
public class BasketReminderLog
{
    public int Id { get; set; }

    public Guid BasketId { get; set; }

    public virtual Basket Basket { get; set; } = null!;

    public string BasketExternalId { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public int CustomerId { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public int ReminderTimeForHours { get; set; }

    /// <summary>
    /// Hangi schedule'dan gönderildiği (nullable - geriye dönük uyumluluk için)
    /// </summary>
    public int? BasketReminderScheduleId { get; set; }

    public virtual BasketReminderSchedule? BasketReminderSchedule { get; set; }

    public DateTime SentAt { get; set; } = DateTime.Now;

    public bool IsSuccessful { get; set; } = false;

    public string? ErrorMessage { get; set; }

    public string? NotificationContent { get; set; }

    // Helper properties
    [NotMapped]
    public string StatusText => IsSuccessful ? "Başarılı" : "Başarısız";

    [NotMapped]
    public string StatusBadgeClass => IsSuccessful ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";

    [NotMapped]
    public string ReminderTimeText
    {
        get
        {
            return ReminderTimeForHours switch
            {
                6 => "6 Saat",
                24 => "1 Gün",
                168 => "1 Hafta",
                _ => $"{ReminderTimeForHours} Saat"
            };
        }
    }
}

/// <summary>
/// Sepet hatırlatma modülü global ayarları
/// </summary>
public class BasketReminderSettings
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public int MaxNotificationsPerCustomer { get; set; } = 2; // Maksimum 1-2 bildirim

    public string NotificationContent { get; set; } = string.Empty; // Maksimum 300 karakter

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string UpdatedByUserId { get; set; } = string.Empty;

    public virtual ApplicationUser UpdatedByUser { get; set; } = null!;

    // Helper properties
    [NotMapped]
    public string StatusText => IsEnabled ? "Aktif" : "Pasif";

    [NotMapped]
    public string StatusBadgeClass => IsEnabled ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";
}
