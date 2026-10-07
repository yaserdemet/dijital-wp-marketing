using System.ComponentModel.DataAnnotations;

namespace PushDashboard.DTOs;

/// <summary>
/// Aktif iletişim kanalı bilgileri
/// </summary>
public class ActiveChannelDto
{
    public string ChannelType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public bool IsEnabled { get; set; }
    public decimal CostPerMessage { get; set; }
    public string? ConfigurationStatus { get; set; }
    public string? LastTestResult { get; set; }
    public DateTime? LastTestedAt { get; set; }
}

/// <summary>
/// İlk alışveriş modülü ayarları
/// </summary>
public class FirstOrderSettingsDto
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

    [Display(Name = "Hediye Çeki Etkin")]
    public bool GiftVoucherEnabled { get; set; }

    [Display(Name = "Hediye Çeki Tutarı")]
    [Range(1, 10000, ErrorMessage = "Hediye çeki tutarı 1-10000 arasında olmalıdır")]
    public decimal GiftVoucherAmount { get; set; } = 50;

    [Display(Name = "İndirim Tipi")]
    public int GiftVoucherDiscountType { get; set; } = 1; // 1: Sabit tutar, 2: Yüzde

    [Display(Name = "Geçerlilik Süresi (Gün)")]
    [Range(1, 365, ErrorMessage = "Geçerlilik süresi 1-365 gün arasında olmalıdır")]
    public int GiftVoucherValidityDays { get; set; } = 30;

    public List<ActiveChannelDto> AvailableChannels { get; set; } = new();
    public bool HasActiveEcommerceIntegration { get; set; }
    public string? ActiveEcommercePlatform { get; set; }

    // Template bilgileri
    public int? EmailTemplateId { get; set; }
    public string? EmailTemplateName { get; set; }
    public int? SmsTemplateId { get; set; }
    public string? SmsTemplateName { get; set; }
    public string? WhatsappTemplateName { get; set; }
}

/// <summary>
/// İlk alışveriş modülü ayarları güncelleme request'i
/// </summary>
public class UpdateFirstOrderSettingsRequest
{
    [Required]
    public bool Enabled { get; set; }

    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool WhatsAppEnabled { get; set; }
    public bool PushEnabled { get; set; }

    public bool GiftVoucherEnabled { get; set; }

    [Range(1, 10000, ErrorMessage = "Hediye çeki tutarı 1-10000 arasında olmalıdır")]
    public decimal GiftVoucherAmount { get; set; } = 50;

    [Range(1, 2, ErrorMessage = "İndirim tipi 1 (Sabit tutar) veya 2 (Yüzde) olmalıdır")]
    public int GiftVoucherDiscountType { get; set; } = 1;

    [Range(1, 365, ErrorMessage = "Geçerlilik süresi 1-365 gün arasında olmalıdır")]
    public int GiftVoucherValidityDays { get; set; } = 30;

    // Template bilgileri
    public int? EmailTemplateId { get; set; }
    public string? EmailTemplateName { get; set; }
    public int? SmsTemplateId { get; set; }
    public string? SmsTemplateName { get; set; }
    public string? WhatsAppTemplateName { get; set; }
}

/// <summary>
/// Hediye çeki test sonucu
/// </summary>
public class GiftVoucherTestResult
{
    public bool Success { get; set; }
    public string? VoucherCode { get; set; }
    public string? Amount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Platform { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// İlk alışveriş modülü istatistikleri
/// </summary>
public class FirstOrderStatsDto
{
    public int TotalFirstOrderNotifications { get; set; }
    public int TotalGiftVouchersCreated { get; set; }
    public decimal TotalCostThisMonth { get; set; }
    public int NotificationsThisMonth { get; set; }
    public Dictionary<string, int> ChannelUsageStats { get; set; } = new();
    public DateTime? LastNotificationSent { get; set; }
}

/// <summary>
/// İlk alışveriş mesaj log detayları
/// </summary>
public class FirstOrderMessageLogDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string ChannelDisplayName { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal Cost { get; set; }
    public DateTime SentAt { get; set; }
    public string? GiftVoucherCode { get; set; }
    public string? GiftVoucherAmount { get; set; }
    public string? OrderNumber { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal? OrderAmount { get; set; }
    public string? OrderCurrency { get; set; }

    // UI helpers
    public string StatusText => IsSuccessful ? "Başarılı" : "Başarısız";
    public string StatusBadgeClass => IsSuccessful ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";
    public string ChannelIconClass => Channel?.ToLower() switch
    {
        "email" => "fa-envelope",
        "whatsapp" => "fa-whatsapp",
        "sms" => "fa-sms",
        "push" => "fa-bell",
        _ => "fa-paper-plane"
    };
    public string ChannelBgClass => Channel?.ToLower() switch
    {
        "email" => "bg-blue-100 text-blue-800",
        "whatsapp" => "bg-green-100 text-green-800",
        "sms" => "bg-purple-100 text-purple-800",
        "push" => "bg-yellow-100 text-yellow-800",
        _ => "bg-gray-100 text-gray-800"
    };
}

/// <summary>
/// İlk alışverişini yapmış müşteri bilgileri
/// </summary>
public class FirstOrderCustomerDto
{
    public int CustomerId { get; set; }
    public int CustomerExternalId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    
    // Sipariş bilgileri
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal OrderAmount { get; set; }
    public string OrderCurrency { get; set; } = "TRY";
    
    // İletişim izinleri
    public bool EmailPermission { get; set; }
    public bool SmsPermission { get; set; }
    
    // WhatsApp izni (SMS izni varsa WhatsApp da var)
    public bool WhatsAppPermission => SmsPermission;
    
    // Başarılı gönderilen kanallar
    public bool EmailSent { get; set; }
    public bool SmsSent { get; set; }
    public bool WhatsAppSent { get; set; }
    
    // Hediye çeki bilgileri
    public string? GiftVoucherCode { get; set; }
    public decimal? GiftVoucherAmount { get; set; }
    
    // Sipariş detayları (modal için)
    public List<FirstOrderProductDto> Products { get; set; } = new();
}

/// <summary>
/// İlk alışveriş ürün bilgileri
/// </summary>
public class FirstOrderProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}