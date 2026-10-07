using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Şirket bazlı stok hatırlatma widget ayarları (müşteri ön yüzü popup)
/// </summary>
public class StockReminderWidgetSettings
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    // İçerik Ayarları
    public string Title { get; set; } = "Bedenini Seç, Stokta Olunca Sana Bildirelim!";

    public string Description { get; set; } = "Ürün stoğa girdiğinde size WhatsApp veya SMS ile hemen bilgi vereceğiz. Telefon numaranızın doğru olduğundan emin olun!";

    public string SubmitButtonText { get; set; } = "Kaydet";

    public string PrivacyText { get; set; } = "Kampanya, duyuru ve bilgilendirmelerden WhatsApp ve SMS yoluyla haberdar olmak istediğimi belirtiyorum.";

    public string PhonePlaceholder { get; set; } = "5XXXXXXXXX";

    public string EmailPlaceholder { get; set; } = "E-posta adresiniz (isteğe bağlı)";

    // Varyasyon / Beden Seçimi
    /// <summary>
    /// Virgülle ayrılmış varyasyon seçenekleri. Boşsa beden seçimi gösterilmez.
    /// Örnek: "S,M,L,XL" veya "36,37,38,39,40"
    /// </summary>
    public string VariantOptions { get; set; } = "S,M,L,XL";

    public string VariantLabel { get; set; } = "Beden Seçin";

    public bool ShowVariantSelector { get; set; } = true;

    // Giriş Alanları
    public bool ShowPhoneField { get; set; } = true;

    public bool ShowEmailField { get; set; } = false;

    /// <summary>
    /// Ad ve soyad alanlarını göster (isteğe bağlı)
    /// </summary>
    public bool ShowNameFields { get; set; } = false;

    public bool RequirePhone { get; set; } = true;

    public bool ShowConsentCheckbox { get; set; } = true;

    // Tasarım Ayarları
    public string BackgroundColor { get; set; } = "#ffffff";

    public string TextColor { get; set; } = "#333333";

    public string TitleColor { get; set; } = "#e53935";

    public string ButtonColor { get; set; } = "#e53935";

    public string ButtonTextColor { get; set; } = "#ffffff";

    public string VariantBorderColor { get; set; } = "#e5e7eb";

    public string VariantSelectedColor { get; set; } = "#111111";

    public string BorderRadius { get; set; } = "16px";

    // Genel Ayarlar
    public bool IsActive { get; set; } = true;

    public bool ShowOnMobile { get; set; } = true;

    public bool ShowOnDesktop { get; set; } = true;

    public bool EnableAnimation { get; set; } = true;

    // Tracking Sayaçları
    public int ImpressionCount { get; set; } = 0;

    public int SubmitCount { get; set; } = 0;

    public int DismissCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string UpdatedByUserId { get; set; } = string.Empty;

    public virtual ApplicationUser UpdatedByUser { get; set; } = null!;

    // Navigation Properties
    public virtual ICollection<StockReminderWidgetSubmission> Submissions { get; set; } = new List<StockReminderWidgetSubmission>();
}

/// <summary>
/// Stok hatırlatma abonelik kaydı — hem widget hem Ticimax kaynağından gelen tüm abonelikleri tutar.
/// Tek tablo, tek kaynak (single source of truth).
/// </summary>
public class StockReminderWidgetSubmission
{
    public int Id { get; set; }

    /// <summary>
    /// Widget ayarları FK — widget kaynağı için dolu, ticimax kaynağı için null
    /// </summary>
    public int? StockReminderWidgetSettingsId { get; set; }

    public virtual StockReminderWidgetSettings? StockReminderWidgetSettings { get; set; }

    public Guid CompanyId { get; set; }

    // ── Kaynak Bilgisi ──

    /// <summary>
    /// Kaynak: "widget" veya "ticimax"
    /// </summary>
    public string Source { get; set; } = "widget";

    /// <summary>
    /// Ticimax stok alarm ürün ID (widget için 0)
    /// </summary>
    public int ExternalAlarmId { get; set; }

    /// <summary>
    /// Ticimax üye ID (widget için 0)
    /// </summary>
    public int ExternalMemberId { get; set; }

    // ── Ürün Bilgileri ──

    /// <summary>
    /// Ürün ID (e-ticaret sistemindeki)
    /// </summary>
    public string? ProductId { get; set; }

    /// <summary>
    /// Ürün adı
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// Stok kodu
    /// </summary>
    public string? StockCode { get; set; }

    /// <summary>
    /// Toplam stok adedi
    /// </summary>
    public int TotalStock { get; set; }

    /// <summary>
    /// Ürün fiyatı (KDV hariç)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Ürün fiyatı (KDV dahil)
    /// </summary>
    public decimal PriceWithTax { get; set; }

    /// <summary>
    /// Para birimi
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Ürün resim URL'si
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Ürün sayfa URL'si
    /// </summary>
    public string? ProductUrl { get; set; }

    // ── Abone (Müşteri) Bilgileri ──

    /// <summary>
    /// Seçilen varyasyon/beden
    /// </summary>
    public string? SelectedVariant { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    /// <summary>
    /// Müşteri adı
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Müşteri soyadı
    /// </summary>
    public string? LastName { get; set; }

    public string? PageUrl { get; set; }

    public string? SessionId { get; set; }

    public bool ConsentGiven { get; set; } = false;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    // ── Bildirim Takibi ──

    /// <summary>
    /// Bildirim gönderildi mi?
    /// </summary>
    public bool IsNotified { get; set; } = false;

    /// <summary>
    /// Bildirim gönderim tarihi
    /// </summary>
    public DateTime? NotifiedAt { get; set; }

    // ── Tarihler ──

    /// <summary>
    /// Son senkronizasyon tarihi
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
