using System.ComponentModel.DataAnnotations;

namespace PushDashboard.ViewModels;

/// <summary>
/// Sipariş durumu bildirim ayarları ana sayfa view model'i
/// </summary>
public class OrderStatusNotificationIndexViewModel
{
    public List<OrderStatusSelectItem> OrderStatuses { get; set; } = new();
    public List<OrderStatusNotificationViewModel> Notifications { get; set; } = new();
    public List<TemplateSelectItem> EmailTemplates { get; set; } = new();
    public List<TemplateSelectItem> SmsTemplates { get; set; } = new();
    public List<TemplateSelectItem> WhatsAppTemplates { get; set; } = new();
    public ActiveChannelsViewModel ActiveChannels { get; set; } = new();
}

/// <summary>
/// Sipariş durumu bildirim ayarları view model'i
/// </summary>
public class OrderStatusNotificationViewModel
{
    public string OrderStatus { get; set; } = string.Empty;
    public string OrderStatusDisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Email ayarları
    public bool EmailNotificationEnabled { get; set; }
    public int? EmailTemplateId { get; set; }

    // SMS ayarları
    public bool SmsNotificationEnabled { get; set; }
    public int? SmsTemplateId { get; set; }

    // WhatsApp ayarları
    public bool WhatsAppNotificationEnabled { get; set; }
    public string? WhatsAppTemplateId { get; set; }

    // Genel ayarlar
    public int DelayMinutes { get; set; }
    public int TotalNotificationsSent { get; set; }
    public DateTime? LastNotificationAt { get; set; }

    // Computed properties
    public string FormattedLastNotificationAt => LastNotificationAt?.ToString("dd.MM.yyyy HH:mm") ?? "Hiç gönderilmedi";
    public bool HasAnyNotificationEnabled => EmailNotificationEnabled || SmsNotificationEnabled || WhatsAppNotificationEnabled;
}

/// <summary>
/// Sipariş durumu değişiklik geçmişi view model'i
/// </summary>
public class OrderStatusChangeLogViewModel
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string? OrderNumber { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? NewStatusDisplayName { get; set; }
    public DateTime StatusChangedAt { get; set; }
    public bool NotificationSent { get; set; }
    public string? NotificationChannels { get; set; }

    // Computed properties
    public string FormattedStatusChangedAt => StatusChangedAt.ToString("dd.MM.yyyy HH:mm");
    public string StatusChangeText => string.IsNullOrEmpty(OldStatus)
        ? $"Durum: {NewStatusDisplayName}"
        : $"{GetStatusDisplayName(OldStatus)} → {NewStatusDisplayName}";
    public List<string> NotificationChannelsList => string.IsNullOrEmpty(NotificationChannels)
        ? new List<string>()
        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(NotificationChannels) ?? new List<string>();

    private string GetStatusDisplayName(string status)
    {
        return status switch
        {
            "1" => "Beklemede",
            "2" => "Onaylandı",
            "3" => "Hazırlanıyor",
            "4" => "Kargoya Verildi",
            "5" => "Teslim Edildi",
            "6" => "İptal Edildi",
            "7" => "İade Edildi",
            _ => status
        };
    }
}

/// <summary>
/// Sipariş durumu seçim item'ı
/// </summary>
public class OrderStatusSelectItem
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Şablon seçim item'ı
/// </summary>
public class TemplateSelectItem
{
    public string WhatsappId { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Sipariş durumu bildirim ayarları kaydetme request'i
/// </summary>
public class SaveOrderStatusNotificationRequest
{
    [Required]
    public string OrderStatus { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    // Email ayarları
    public bool EmailNotificationEnabled { get; set; }
    public int? EmailTemplateId { get; set; }

    // SMS ayarları
    public bool SmsNotificationEnabled { get; set; }
    public int? SmsTemplateId { get; set; }

    // WhatsApp ayarları
    public bool WhatsAppNotificationEnabled { get; set; }
    public string? WhatsAppTemplateId { get; set; }

    // Genel ayarlar
    [Range(0, 1440, ErrorMessage = "Gecikme süresi 0-1440 dakika arasında olmalıdır")]
    public int DelayMinutes { get; set; }
}

/// <summary>
/// Bildirim aktif/pasif yapma request'i
/// </summary>
public class ToggleNotificationRequest
{
    [Required]
    public string OrderStatus { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

/// <summary>
/// Sipariş durumu istatistikleri view model'i
/// </summary>
public class OrderStatusStatisticsViewModel
{
    public string OrderStatus { get; set; } = string.Empty;
    public string OrderStatusDisplayName { get; set; } = string.Empty;
    public int TotalChanges { get; set; }
    public int NotificationsSent { get; set; }
    public int NotificationsFailed { get; set; }
    public DateTime? LastChangeAt { get; set; }
    public DateTime? LastNotificationAt { get; set; }

    // Computed properties
    public string FormattedLastChangeAt => LastChangeAt?.ToString("dd.MM.yyyy HH:mm") ?? "Hiç değişmedi";
    public string FormattedLastNotificationAt => LastNotificationAt?.ToString("dd.MM.yyyy HH:mm") ?? "Hiç gönderilmedi";
    public double NotificationSuccessRate => TotalChanges > 0 ? (double)NotificationsSent / TotalChanges * 100 : 0;
    public string FormattedSuccessRate => $"{NotificationSuccessRate:F1}%";
}

/// <summary>
/// Sipariş durumu eşleştirme view model'i
/// </summary>
public class OrderStatusMappingViewModel
{
    public int Id { get; set; }
    public string IntegrationType { get; set; } = string.Empty;
    public string ExternalStatus { get; set; } = string.Empty;
    public string ExternalStatusDisplayName { get; set; } = string.Empty;
    public string InternalStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// E-ticaret platform durumu
/// </summary>
public class ExternalOrderStatusViewModel
{
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Sipariş durumu eşleştirme ayarları view model'i
/// </summary>
public class OrderStatusMappingSettingsViewModel
{
    public string IntegrationType { get; set; } = string.Empty;
    public List<ExternalOrderStatusViewModel> ExternalStatuses { get; set; } = new();
    public List<OrderStatusSelectItem> InternalStatuses { get; set; } = new();
    public List<OrderStatusMappingViewModel> CurrentMappings { get; set; } = new();
}

/// <summary>
/// Sipariş durumu bildirim test request'i
/// </summary>
public class TestOrderStatusNotificationRequest
{
    [Required]
    public string OrderStatus { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string TestEmail { get; set; } = string.Empty;

    public string? TestPhone { get; set; }

    [Required]
    public string TestOrderId { get; set; } = string.Empty;

    public string? TestOrderNumber { get; set; }

    public string? TestCustomerName { get; set; }

    public decimal? TestOrderAmount { get; set; }

    public List<string> TestChannels { get; set; } = new();
}

/// <summary>
/// Aktif iletişim kanalları view model'i
/// </summary>
public class ActiveChannelsViewModel
{
    public bool HasEmail { get; set; }
    public bool HasSms { get; set; }
    public bool HasWhatsApp { get; set; }

    public bool HasAnyChannel => HasEmail || HasSms || HasWhatsApp;
    public int ActiveChannelCount => (HasEmail ? 1 : 0) + (HasSms ? 1 : 0) + (HasWhatsApp ? 1 : 0);
}
