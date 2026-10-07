using System.ComponentModel.DataAnnotations;

namespace PushDashboard.DTOs;

/// <summary>
/// Favori ürünler modülü ayarları
/// </summary>
public class FavoriteProductSettingsDto
{
    [Display(Name = "Modül Etkin")]
    public bool Enabled { get; set; }

    [Display(Name = "Email Bildirimi")]
    public bool EmailEnabled { get; set; }

    [Display(Name = "SMS Bildirimi")]
    public bool SmsEnabled { get; set; }

    [Display(Name = "WhatsApp Bildirimi")]
    public bool WhatsAppEnabled { get; set; } = true;

    [Display(Name = "Yeni Favori Bildirimi")]
    public bool NotifyOnNewFavorite { get; set; } = true;

    [Display(Name = "Yeni Favori Bildirim Mesajı")]
    [MaxLength(500)]
    public string NewFavoriteMessage { get; set; } = "Merhaba {CustomerName}, {ProductName} ürününü favorilerinize eklediniz! Fiyat: {ProductPrice}. Stokta sadece {StockCount} adet kaldı, acele edin! {ProductUrl}";

    [Display(Name = "İndirim Bildirimi")]
    public bool NotifyOnDiscount { get; set; } = true;

    [Display(Name = "Favori Ürün İndirim Mesajı")]
    [MaxLength(500)]
    public string DiscountMessage { get; set; } = "Merhaba {CustomerName}, favorinizdeki {ProductName} ürünü indirimde! Yeni fiyat: {ProductPrice}. Hemen sipariş verin: {ProductUrl}";

    // Channel-specific template settings
    public string? EmailTemplate { get; set; }
    public string? EmailTemplateName { get; set; }
    public string? SmsTemplate { get; set; }
    public string? SmsTemplateName { get; set; }
    public string? WhatsappTemplate { get; set; }
    public string? WhatsappTemplateName { get; set; }

    // Available channels for UI
    public List<ActiveChannelDto> AvailableChannels { get; set; } = new();

    public bool HasActiveEcommerceIntegration { get; set; }
    public string? ActiveEcommercePlatform { get; set; }
}

/// <summary>
/// Favori ürünler modülü ayarları güncelleme request'i
/// </summary>
public class UpdateFavoriteProductSettingsRequest
{
    [Required]
    public bool Enabled { get; set; }

    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool WhatsAppEnabled { get; set; } = true;

    public bool NotifyOnNewFavorite { get; set; } = true;

    [MaxLength(500, ErrorMessage = "Bildirim mesajı en fazla 500 karakter olmalıdır")]
    public string NewFavoriteMessage { get; set; } = "Merhaba {CustomerName}, {ProductName} ürününü favorilerinize eklediniz! Fiyat: {ProductPrice}. Stokta sadece {StockCount} adet kaldı, acele edin! {ProductUrl}";

    public bool NotifyOnDiscount { get; set; } = true;

    [MaxLength(500, ErrorMessage = "Bildirim mesajı en fazla 500 karakter olmalıdır")]
    public string DiscountMessage { get; set; } = "Merhaba {CustomerName}, favorinizdeki {ProductName} ürünü indirimde! Yeni fiyat: {ProductPrice}. Hemen sipariş verin: {ProductUrl}";

    // Channel-specific template settings
    public string? EmailTemplate { get; set; }
    public string? EmailTemplateName { get; set; }
    public string? SmsTemplate { get; set; }
    public string? SmsTemplateName { get; set; }
    public string? WhatsAppTemplate { get; set; }
    public string? WhatsAppTemplateName { get; set; }
}

/// <summary>
/// Manuel bildirim gönderme request'i
/// </summary>
public class SendFavoriteNotificationRequest
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    public string NotificationType { get; set; } = "custom"; // custom, stock_arrived, discount
    
    [MaxLength(500)]
    public string? CustomMessage { get; set; }
}

/// <summary>
/// Toplu bildirim gönderme request'i
/// </summary>
public class SendBulkFavoriteNotificationRequest
{
    [Required]
    public List<int> ProductIds { get; set; } = new();
    
    [Required]
    public string NotificationType { get; set; } = "custom"; // custom, stock_arrived, discount
    
    [MaxLength(500)]
    public string? CustomMessage { get; set; }
}

