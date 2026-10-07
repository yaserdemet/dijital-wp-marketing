using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PushDashboard.Models;

/// <summary>
/// Şirket bazlı çerez yönetimi ayarları
/// </summary>
public class CookieManagement
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    // Banner Ayarları
    public string BannerTitle { get; set; } = "Bu site çerezleri kullanır";

    public string BannerDescription { get; set; } = "Web sitemizde size en iyi deneyimi sunabilmek için çerezleri kullanıyoruz. Çerez kullanımını kabul ederek daha iyi bir deneyim yaşayabilirsiniz.";

    public string AcceptButtonText { get; set; } = "Tümünü Kabul Et";

    public string RejectButtonText { get; set; } = "Tümünü Reddet";

    public string SettingsButtonText { get; set; } = "Ayarları Yönet";

    public string SaveButtonText { get; set; } = "Seçimleri Kaydet";

    // Tasarım Ayarları
    public string BannerPosition { get; set; } = "bottom"; // bottom, top, left, right

    public string BannerBackgroundColor { get; set; } = "#ffffff";

    public string BannerTextColor { get; set; } = "#333333";

    public string AcceptButtonColor { get; set; } = "#4CAF50";

    public string RejectButtonColor { get; set; } = "#f44336";

    public string SettingsButtonColor { get; set; } = "#2196F3";

    public string BorderRadius { get; set; } = "8px";

    public bool ShowSettingsButton { get; set; } = true;

    public bool EnableAnimation { get; set; } = true;

    // Çerez Kategorileri (JSON olarak saklanacak)
    public string CategoriesJson { get; set; } = string.Empty;

    // Genel Ayarlar (Modüle sahipse her zaman aktif)
    public bool IsActive { get; set; } = true;

    public int CookieExpiryDays { get; set; } = 365;

    // Tracking Sayaçları
    public int ImpressionCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string UpdatedByUserId { get; set; } = string.Empty;

    public virtual ApplicationUser UpdatedByUser { get; set; } = null!;

    // Navigation Properties
    public virtual ICollection<CookieConsentEvent> Events { get; set; } = new List<CookieConsentEvent>();

    // Computed property for categories
    [NotMapped]
    public List<CookieCategory> Categories
    {
        get
        {
            if (string.IsNullOrEmpty(CategoriesJson))
                return GetDefaultCategories();

            try
            {
                return JsonSerializer.Deserialize<List<CookieCategory>>(CategoriesJson)
                       ?? GetDefaultCategories();
            }
            catch
            {
                return GetDefaultCategories();
            }
        }
        set
        {
            CategoriesJson = JsonSerializer.Serialize(value);
            UpdatedAt = DateTime.Now;
        }
    }

    private static List<CookieCategory> GetDefaultCategories()
    {
        return new List<CookieCategory>
        {
            new CookieCategory
            {
                Id = "necessary",
                Name = "Gerekli Çerezler",
                Description = "Web sitesinin temel işlevlerini yerine getirmesi için gerekli çerezlerdir.",
                IsRequired = true,
                IsEnabled = true
            },
            new CookieCategory
            {
                Id = "performance",
                Name = "Performans Çerezleri",
                Description = "Web sitesinin performansını analiz etmek ve iyileştirmek için kullanılır.",
                IsRequired = false,
                IsEnabled = false
            },
            new CookieCategory
            {
                Id = "functional",
                Name = "Fonksiyonel Çerezler",
                Description = "Gelişmiş özellikler ve kişiselleştirme için kullanılan çerezlerdir.",
                IsRequired = false,
                IsEnabled = false
            },
            new CookieCategory
            {
                Id = "marketing",
                Name = "Pazarlama Çerezleri",
                Description = "Reklam ve pazarlama faaliyetleri için kullanılan çerezlerdir.",
                IsRequired = false,
                IsEnabled = false
            }
        };
    }
}

/// <summary>
/// Çerez kategorisi modeli
/// </summary>
public class CookieCategory
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = false;

    public bool IsEnabled { get; set; } = false;
}

/// <summary>
/// Çerez onay olayları izleme modeli
/// </summary>
public class CookieConsentEvent
{
    public int Id { get; set; }

    public int CookieManagementId { get; set; }

    public virtual CookieManagement CookieManagement { get; set; } = null!;

    /// <summary>
    /// impression, accept_all, reject_all, save_settings
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Kabul edilen kategori ID'leri (JSON array)
    /// </summary>
    public string? AcceptedCategoriesJson { get; set; }

    public string? PageUrl { get; set; }

    public string? SessionId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool IsMobile { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
