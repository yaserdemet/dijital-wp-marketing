using PushDashboard.Models;

namespace PushDashboard.Services;

public interface INewsletterPopupService
{
    /// <summary>
    /// Şirketin e-bülten popup ayarlarını getirir
    /// </summary>
    Task<NewsletterPopupSettings?> GetNewsletterPopupAsync(Guid companyId);

    /// <summary>
    /// E-bülten popup ayarlarını kaydeder
    /// </summary>
    Task<bool> SaveNewsletterPopupAsync(NewsletterPopupSettings settings);

    /// <summary>
    /// Script için konfigürasyon verisini getirir
    /// </summary>
    Task<NewsletterPopupScriptConfig> GetScriptConfigAsync(Guid companyId);

    /// <summary>
    /// Varsayılan e-bülten popup ayarlarını oluşturur
    /// </summary>
    Task<NewsletterPopupSettings> CreateDefaultSettingsAsync(Guid companyId, string userId);

    /// <summary>
    /// E-bülten popup konfigürasyonunu R2'ye export eder
    /// </summary>
    Task<string> ExportNewsletterPopupToR2Async(Guid companyId);

    /// <summary>
    /// Tracking olayını kaydeder
    /// </summary>
    Task<bool> RecordTrackingEventAsync(NewsletterPopupTrackingModel model, string? ipAddress, string? userAgent);

    /// <summary>
    /// E-bülten popup fotoğrafını R2'ye yükler
    /// </summary>
    Task<string> UploadNewsletterImageAsync(Guid companyId, Stream imageStream, string contentType, string fileName, bool isMobile = false);

    /// <summary>
    /// E-bülten popup fotoğrafını siler
    /// </summary>
    Task<bool> DeleteNewsletterImageAsync(Guid companyId);
}

/// <summary>
/// Script için kullanılacak konfigürasyon modeli
/// </summary>
public class NewsletterPopupScriptConfig
{
    public bool IsActive { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public string PrivacyText { get; set; } = string.Empty;
    public string PrivacyUrl { get; set; } = string.Empty;
    public string? KvkkLink { get; set; }
    public string? EtkLink { get; set; }
    public string NamePlaceholder { get; set; } = string.Empty;
    public string SurnamePlaceholder { get; set; } = string.Empty;
    public string PhonePlaceholder { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string ButtonColor { get; set; } = string.Empty;
    public string ButtonTextColor { get; set; } = string.Empty;
    public string BorderRadius { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string ImagePosition { get; set; } = "left";
    public bool HideImageOnMobile { get; set; }
    public string? MobileImageUrl { get; set; }
    public bool SloganEnabled { get; set; }
    public string? SloganBadgeText { get; set; }
    public string? SloganText { get; set; }
    public string SloganHighlightColor { get; set; } = "#c8a96e";
    public string SloganTextColor { get; set; } = "#ffffff";
    public bool ShowNameFields { get; set; }
    public bool ShowPhoneField { get; set; }
    public bool ShowConsentCheckbox { get; set; }
    public int DelaySeconds { get; set; }
    public int ShowFrequencyDays { get; set; }
    public bool ShowOnMobile { get; set; }
    public bool ShowOnDesktop { get; set; }
    public bool EnableAnimation { get; set; }
}

/// <summary>
/// Tracking event modeli
/// </summary>
public class NewsletterPopupTrackingModel
{
    public Guid CompanyId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public string? SessionId { get; set; }
    public bool IsMobile { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerSurname { get; set; }
    public string? CustomerPhone { get; set; }
}
