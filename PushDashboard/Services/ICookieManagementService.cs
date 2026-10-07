using PushDashboard.Models;
using PushDashboard.Models.ViewModels;

namespace PushDashboard.Services;

public interface ICookieManagementService
{
    /// <summary>
    /// Şirketin çerez yönetimi ayarlarını getirir
    /// </summary>
    Task<CookieManagement?> GetCookieManagementAsync(Guid companyId);

    /// <summary>
    /// Çerez yönetimi ayarlarını kaydeder
    /// </summary>
    Task<bool> SaveCookieManagementAsync(CookieManagement cookieManagement);

    /// <summary>
    /// Script için konfigürasyon verisini getirir
    /// </summary>
    Task<CookieManagementScriptConfig> GetScriptConfigAsync(Guid companyId);

    /// <summary>
    /// Şirketin çerez yönetimi modülüne sahip olup olmadığını kontrol eder
    /// </summary>
    Task<bool> HasCookieManagementModuleAsync(Guid companyId);

    /// <summary>
    /// Varsayılan çerez yönetimi ayarlarını oluşturur
    /// </summary>
    Task<CookieManagement> CreateDefaultSettingsAsync(Guid companyId, string userId);

    /// <summary>
    /// Çerez yönetimi konfigürasyonunu R2'ye export eder
    /// </summary>
    Task<string> ExportCookieManagementToR2Async(Guid companyId);

    /// <summary>
    /// Çerez onay olayını kaydeder
    /// </summary>
    Task<bool> RecordCookieTrackingEventAsync(CookieManagementTrackingViewModel model, string? ipAddress, string? userAgent);

    /// <summary>
    /// Çerez yönetimi istatistiklerini getirir
    /// </summary>
    Task<CookieManagementStatsViewModel> GetCookieStatsAsync(Guid companyId);
}

/// <summary>
/// Script için kullanılacak konfigürasyon modeli
/// </summary>
public class CookieManagementScriptConfig
{
    public bool IsActive { get; set; }
    public string BannerTitle { get; set; } = string.Empty;
    public string BannerDescription { get; set; } = string.Empty;
    public string AcceptButtonText { get; set; } = string.Empty;
    public string RejectButtonText { get; set; } = string.Empty;
    public string SettingsButtonText { get; set; } = string.Empty;
    public string SaveButtonText { get; set; } = string.Empty;
    public string BannerPosition { get; set; } = string.Empty;
    public string BannerBackgroundColor { get; set; } = string.Empty;
    public string BannerTextColor { get; set; } = string.Empty;
    public string AcceptButtonColor { get; set; } = string.Empty;
    public string RejectButtonColor { get; set; } = string.Empty;
    public string SettingsButtonColor { get; set; } = string.Empty;
    public string BorderRadius { get; set; } = string.Empty;
    public bool ShowSettingsButton { get; set; }
    public bool EnableAnimation { get; set; }
    public int CookieExpiryDays { get; set; }
    public List<CookieCategory> Categories { get; set; } = new();
}
