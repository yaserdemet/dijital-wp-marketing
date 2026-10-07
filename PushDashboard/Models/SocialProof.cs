using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PushDashboard.Models;

/// <summary>
/// Social Proof ayarları - Şirket bazında
/// </summary>
public class SocialProofSettings
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid CompanyId { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// Modül aktif/pasif durumu
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// "İnceleyenler" modülü aktif mi?
    /// </summary>
    public bool IsViewersActive { get; set; } = true;

    /// <summary>
    /// "İnceleyenler" için minimum sayı
    /// </summary>
    [Range(1, 1000)]
    public int ViewersMin { get; set; } = 15;

    /// <summary>
    /// "İnceleyenler" için maksimum sayı
    /// </summary>
    [Range(1, 1000)]
    public int ViewersMax { get; set; } = 45;

    /// <summary>
    /// "Takip edenler" modülü aktif mi?
    /// </summary>
    public bool IsFollowersActive { get; set; } = true;

    /// <summary>
    /// "Takip edenler" için minimum sayı
    /// </summary>
    [Range(1, 1000)]
    public int FollowersMin { get; set; } = 5;

    /// <summary>
    /// "Takip edenler" için maksimum sayı
    /// </summary>
    [Range(1, 1000)]
    public int FollowersMax { get; set; } = 20;

    /// <summary>
    /// "Satın almaya hazırlananlar" modülü aktif mi?
    /// </summary>
    public bool IsBuyersActive { get; set; } = true;

    /// <summary>
    /// "Satın almaya hazırlananlar" için minimum sayı
    /// </summary>
    [Range(1, 1000)]
    public int BuyersMin { get; set; } = 2;

    /// <summary>
    /// "Satın almaya hazırlananlar" için maksimum sayı
    /// </summary>
    [Range(1, 1000)]
    public int BuyersMax { get; set; } = 8;

    /// <summary>
    /// Güncelleme sıklığı (saniye cinsinden)
    /// </summary>
    [Range(30, 300)]
    public int UpdateInterval { get; set; } = 60;

    /// <summary>
    /// Popup gösterim süresi (saniye cinsinden)
    /// </summary>
    [Range(3, 15)]
    public int DisplayDuration { get; set; } = 5;

    /// <summary>
    /// Metin şablonları (JSON formatında)
    /// </summary>
    [Column(TypeName = "text")]
    public string? TextTemplatesJson { get; set; }

    /// <summary>
    /// Görsel ayarlar (JSON formatında)
    /// </summary>
    [Column(TypeName = "text")]
    public string? DisplaySettingsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Required]
    [StringLength(450)]
    public string UpdatedByUserId { get; set; } = string.Empty;

    [ForeignKey("UpdatedByUserId")]
    public virtual ApplicationUser UpdatedByUser { get; set; } = null!;

    // Helper properties for JSON serialization/deserialization
    [NotMapped]
    public SocialProofTextTemplates TextTemplates
    {
        get
        {
            if (string.IsNullOrEmpty(TextTemplatesJson))
                return new SocialProofTextTemplates();

            try
            {
                return JsonSerializer.Deserialize<SocialProofTextTemplates>(TextTemplatesJson)
                       ?? new SocialProofTextTemplates();
            }
            catch
            {
                return new SocialProofTextTemplates();
            }
        }
        set
        {
            TextTemplatesJson = JsonSerializer.Serialize(value);
            UpdatedAt = DateTime.Now;
        }
    }

    [NotMapped]
    public SocialProofDisplaySettings DisplaySettings
    {
        get
        {
            if (string.IsNullOrEmpty(DisplaySettingsJson))
                return new SocialProofDisplaySettings();

            try
            {
                return JsonSerializer.Deserialize<SocialProofDisplaySettings>(DisplaySettingsJson)
                       ?? new SocialProofDisplaySettings();
            }
            catch
            {
                return new SocialProofDisplaySettings();
            }
        }
        set
        {
            DisplaySettingsJson = JsonSerializer.Serialize(value);
            UpdatedAt = DateTime.Now;
        }
    }
}

/// <summary>
/// Metin şablonları
/// </summary>
public class SocialProofTextTemplates
{
    public string ViewersTemplate { get; set; } = "{count} kişi şu anda bu ürünü inceliyor";
    public string FollowersTemplate { get; set; } = "{count} kişi bu ürünü takip ediyor";
    public string BuyersTemplate { get; set; } = "{count} kişi satın almaya hazırlanıyor";
}

/// <summary>
/// Görsel ayarlar
/// </summary>
public class SocialProofDisplaySettings
{
    /// <summary>
    /// Pozisyon: "bottom-left", "bottom-right", "top-left", "top-right"
    /// </summary>
    public string Position { get; set; } = "bottom-left";

    /// <summary>
    /// Ana renk (hex formatında)
    /// </summary>
    public string PrimaryColor { get; set; } = "#007bff";

    /// <summary>
    /// Metin rengi (hex formatında)
    /// </summary>
    public string TextColor { get; set; } = "#ffffff";

    /// <summary>
    /// Arka plan rengi (hex formatında)
    /// </summary>
    public string BackgroundColor { get; set; } = "#333333";

    /// <summary>
    /// Border radius (px cinsinden)
    /// </summary>
    public int BorderRadius { get; set; } = 8;

    /// <summary>
    /// Font boyutu (px cinsinden)
    /// </summary>
    public int FontSize { get; set; } = 14;

    /// <summary>
    /// Animasyon aktif/pasif
    /// </summary>
    public bool EnableAnimation { get; set; } = true;

    /// <summary>
    /// Gölge efekti aktif/pasif
    /// </summary>
    public bool EnableShadow { get; set; } = true;
}
