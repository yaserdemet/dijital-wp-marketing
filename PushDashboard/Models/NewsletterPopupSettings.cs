using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Şirket bazlı e-bülten popup ayarları
/// </summary>
public class NewsletterPopupSettings
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    // İçerik Ayarları
    public string Title { get; set; } = "Bültenimize Abone Olun, Fırsatları İlk Siz Yakalayın!";

    public string Description { get; set; } = "İndirim ve fırsatlardan ilk sizin haberiniz olsun, kayıt olun ve avantajlardan yararlanın!";

    public string SubmitButtonText { get; set; } = "Kaydet";

    public string PrivacyText { get; set; } = "Kampanya, duyuru ve bilgilendirmelerden E-Posta, WhatsApp ve SMS yoluyla haberdar olmak istediğimi belirtiyorum.";

    public string PrivacyUrl { get; set; } = "#";

    public string NamePlaceholder { get; set; } = "Adınızı Giriniz";

    public string SurnamePlaceholder { get; set; } = "Soyadınızı Giriniz";

    public string PhonePlaceholder { get; set; } = "5XXXXXXXXX";

    // Tasarım Ayarları
    public string BackgroundColor { get; set; } = "#ffffff";

    public string TextColor { get; set; } = "#333333";

    public string ButtonColor { get; set; } = "#e53935";

    public string ButtonTextColor { get; set; } = "#ffffff";

    public string BorderRadius { get; set; } = "12px";

    // Fotoğraf Ayarları
    /// <summary>
    /// Opsiyonel popup fotoğrafı URL'i (R2'de saklanır)
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Fotoğrafın konumu: left, top, right
    /// </summary>
    public string ImagePosition { get; set; } = "left";

    /// <summary>
    /// Mobilde fotoğrafı gizle
    /// </summary>
    public bool HideImageOnMobile { get; set; } = false;

    /// <summary>
    /// Mobil için özel fotoğraf URL'i (ayarlanmazsa masaüstü görseli kullanılır)
    /// </summary>
    public string? MobileImageUrl { get; set; }

    // Slogan Ayarları (Fotoğraf üzerinde gösterilir)
    /// <summary>
    /// Slogan özelliğini etkinleştir/devre dışı bırak
    /// </summary>
    public bool SloganEnabled { get; set; } = false;

    /// <summary>
    /// Slogan üstündeki rozet metni (örn: "✦ YENİ KOLEKSİYON")
    /// </summary>
    public string? SloganBadgeText { get; set; }

    /// <summary>
    /// Slogan metni (highlight edilecek kelimeler [hl]...[/hl] ile sarılır)
    /// </summary>
    public string? SloganText { get; set; }

    /// <summary>
    /// Highlight edilen kelimelerin rengi
    /// </summary>
    public string SloganHighlightColor { get; set; } = "#c8a96e";

    /// <summary>
    /// Slogan metin rengi
    /// </summary>
    public string SloganTextColor { get; set; } = "#ffffff";

    // Alan Görünürlüğü
    public bool ShowNameFields { get; set; } = true;

    public bool ShowPhoneField { get; set; } = true;

    public bool ShowConsentCheckbox { get; set; } = true;

    // Davranış Ayarları
    public int DelaySeconds { get; set; } = 5;

    public int ShowFrequencyDays { get; set; } = 30;

    public bool ShowOnMobile { get; set; } = true;

    public bool ShowOnDesktop { get; set; } = true;

    public bool EnableAnimation { get; set; } = true;

    // Genel Ayarlar
    public bool IsActive { get; set; } = true;

    // Tracking Sayaçları
    public int ImpressionCount { get; set; } = 0;

    public int SubmitCount { get; set; } = 0;

    public int DismissCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string UpdatedByUserId { get; set; } = string.Empty;

    public virtual ApplicationUser UpdatedByUser { get; set; } = null!;

    // Navigation Properties
    public virtual ICollection<NewsletterPopupEvent> Events { get; set; } = new List<NewsletterPopupEvent>();
}

/// <summary>
/// E-bülten popup olayları izleme modeli
/// </summary>
public class NewsletterPopupEvent
{
    public int Id { get; set; }

    public int NewsletterPopupSettingsId { get; set; }

    public virtual NewsletterPopupSettings NewsletterPopupSettings { get; set; } = null!;

    /// <summary>
    /// impression, submit, dismiss
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    public string? PageUrl { get; set; }

    public string? SessionId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool IsMobile { get; set; }

    // Submit edildiğinde doldurulan alanlar
    public string? CustomerName { get; set; }

    public string? CustomerSurname { get; set; }

    public string? CustomerPhone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
