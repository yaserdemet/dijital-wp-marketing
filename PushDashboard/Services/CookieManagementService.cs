using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Models.ViewModels;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PushDashboard.Services;

public class CookieManagementService : ICookieManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CookieManagementService> _logger;
    private readonly IR2StorageService _r2StorageService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public CookieManagementService(
        ApplicationDbContext context,
        ILogger<CookieManagementService> logger,
        IR2StorageService r2StorageService,
        IConfiguration configuration,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _logger = logger;
        _r2StorageService = r2StorageService;
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<CookieManagement?> GetCookieManagementAsync(Guid companyId)
    {
        try
        {
            return await _context.CookieManagements
                .Include(cm => cm.Company)
                .Include(cm => cm.UpdatedByUser)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cookie management for company {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<bool> SaveCookieManagementAsync(CookieManagement cookieManagement)
    {
        try
        {
            var existing = await _context.CookieManagements
                .FirstOrDefaultAsync(cm => cm.CompanyId == cookieManagement.CompanyId);

            if (existing != null)
            {
                // Update existing
                existing.BannerTitle = cookieManagement.BannerTitle;
                existing.BannerDescription = cookieManagement.BannerDescription;
                existing.AcceptButtonText = cookieManagement.AcceptButtonText;
                existing.RejectButtonText = cookieManagement.RejectButtonText;
                existing.SettingsButtonText = cookieManagement.SettingsButtonText;
                existing.SaveButtonText = cookieManagement.SaveButtonText;
                existing.BannerPosition = cookieManagement.BannerPosition;
                existing.BannerBackgroundColor = cookieManagement.BannerBackgroundColor;
                existing.BannerTextColor = cookieManagement.BannerTextColor;
                existing.AcceptButtonColor = cookieManagement.AcceptButtonColor;
                existing.RejectButtonColor = cookieManagement.RejectButtonColor;
                existing.SettingsButtonColor = cookieManagement.SettingsButtonColor;
                existing.BorderRadius = cookieManagement.BorderRadius;
                existing.ShowSettingsButton = cookieManagement.ShowSettingsButton;
                existing.EnableAnimation = cookieManagement.EnableAnimation;
                existing.CategoriesJson = cookieManagement.CategoriesJson;
                existing.IsActive = cookieManagement.IsActive;
                existing.CookieExpiryDays = cookieManagement.CookieExpiryDays;
                existing.UpdatedAt = DateTime.Now;
                existing.UpdatedByUserId = cookieManagement.UpdatedByUserId;

                _context.CookieManagements.Update(existing);
            }
            else
            {
                // Create new
                cookieManagement.CreatedAt = DateTime.Now;
                cookieManagement.UpdatedAt = DateTime.Now;
                _context.CookieManagements.Add(cookieManagement);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving cookie management for company {CompanyId}", cookieManagement.CompanyId);
            return false;
        }
    }

    public async Task<CookieManagementScriptConfig> GetScriptConfigAsync(Guid companyId)
    {
        try
        {
            // Önce modül kontrolü yap
            var hasModule = await HasCookieManagementModuleAsync(companyId);
            if (!hasModule)
            {
                return new CookieManagementScriptConfig { IsActive = false };
            }

            var cookieManagement = await GetCookieManagementAsync(companyId);
            if (cookieManagement == null || !cookieManagement.IsActive)
            {
                return new CookieManagementScriptConfig { IsActive = false };
            }

            return new CookieManagementScriptConfig
            {
                IsActive = cookieManagement.IsActive,
                BannerTitle = cookieManagement.BannerTitle,
                BannerDescription = cookieManagement.BannerDescription,
                AcceptButtonText = cookieManagement.AcceptButtonText,
                RejectButtonText = cookieManagement.RejectButtonText,
                SettingsButtonText = cookieManagement.SettingsButtonText,
                SaveButtonText = cookieManagement.SaveButtonText,
                BannerPosition = cookieManagement.BannerPosition,
                BannerBackgroundColor = cookieManagement.BannerBackgroundColor,
                BannerTextColor = cookieManagement.BannerTextColor,
                AcceptButtonColor = cookieManagement.AcceptButtonColor,
                RejectButtonColor = cookieManagement.RejectButtonColor,
                SettingsButtonColor = cookieManagement.SettingsButtonColor,
                BorderRadius = cookieManagement.BorderRadius,
                ShowSettingsButton = cookieManagement.ShowSettingsButton,
                EnableAnimation = cookieManagement.EnableAnimation,
                CookieExpiryDays = cookieManagement.CookieExpiryDays,
                Categories = cookieManagement.Categories
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting script config for company {CompanyId}", companyId);
            return new CookieManagementScriptConfig { IsActive = false };
        }
    }

    public async Task<bool> HasCookieManagementModuleAsync(Guid companyId)
    {
        try
        {
            var cookieModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Çerez Yönetimi" && m.IsActive);

            if (cookieModule == null)
                return false;

            return await _context.CompanyModules
                .AnyAsync(cm => cm.CompanyId == companyId && 
                               cm.ModuleId == cookieModule.Id && 
                               cm.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cookie management module for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<CookieManagement> CreateDefaultSettingsAsync(Guid companyId, string userId)
    {
        var defaultSettings = new CookieManagement
        {
            CompanyId = companyId,
            BannerTitle = "Bu site çerezleri kullanır",
            BannerDescription = "Web sitemizde size en iyi deneyimi sunabilmek için çerezleri kullanıyoruz. Çerez kullanımını kabul ederek daha iyi bir deneyim yaşayabilirsiniz.",
            AcceptButtonText = "Tümünü Kabul Et",
            RejectButtonText = "Tümünü Reddet",
            SettingsButtonText = "Ayarları Yönet",
            SaveButtonText = "Seçimleri Kaydet",
            BannerPosition = "bottom",
            BannerBackgroundColor = "#ffffff",
            BannerTextColor = "#333333",
            AcceptButtonColor = "#4CAF50",
            RejectButtonColor = "#f44336",
            SettingsButtonColor = "#2196F3",
            BorderRadius = "8px",
            ShowSettingsButton = true,
            EnableAnimation = true,
            IsActive = true,
            CookieExpiryDays = 365,
            UpdatedByUserId = userId
        };

        // Varsayılan kategorileri ayarla
        defaultSettings.Categories = defaultSettings.Categories; // Bu GetDefaultCategories() çağırır

        return defaultSettings;
    }

    public async Task<string> ExportCookieManagementToR2Async(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Starting R2 export for cookie management. CompanyId: {CompanyId}", companyId);

            var config = await GetScriptConfigAsync(companyId);
            if (!config.IsActive)
            {
                _logger.LogWarning("Cookie management is not active for company {CompanyId}", companyId);
                return "Cookie management is not active";
            }

            var allData = new
            {
                CompanyId = companyId,
                UpdatedAt = DateTime.Now,
                Config = config
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(allData, jsonOptions);

            // JSON dosyasını R2'ye yükle
            var jsonKey = $"{companyId}/cookieManagement.json";
            var jsonUrl = await _r2StorageService.UploadJsonAsync(jsonKey, jsonContent);

            // wwwroot/js/modules'dan JS dosyasını oku ve R2'ye yükle
            var jsFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "js", "modules", "cookie-management-module.js");
            if (File.Exists(jsFilePath))
            {
                var jsScript = await File.ReadAllTextAsync(jsFilePath);
                var jsKey = $"{companyId}/cookieManagement.js";
                var jsUrl = await _r2StorageService.UploadJavaScriptAsync(jsKey, jsScript);

                _logger.LogInformation("Successfully exported cookie management to R2 for company {CompanyId}. JSON: {JsonUrl}, JS: {JsUrl}",
                    companyId, jsonUrl, jsUrl);
            }
            else
            {
                _logger.LogWarning("Cookie management module JS file not found at {Path}", jsFilePath);
            }

            return jsonUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting cookie management to R2 for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<bool> RecordCookieTrackingEventAsync(CookieManagementTrackingViewModel model, string? ipAddress, string? userAgent)
    {
        try
        {
            var cookieManagement = await _context.CookieManagements
                .FirstOrDefaultAsync(cm => cm.CompanyId == model.CompanyId && cm.IsActive);

            if (cookieManagement == null)
                return false;

            bool isMobile = !string.IsNullOrEmpty(userAgent) &&
                Regex.IsMatch(userAgent,
                    @"Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini",
                    RegexOptions.IgnoreCase);

            string? acceptedCategoriesJson = null;
            if (model.AcceptedCategories != null && model.AcceptedCategories.Any())
            {
                acceptedCategoriesJson = JsonSerializer.Serialize(model.AcceptedCategories);
            }

            var trackingEvent = new CookieConsentEvent
            {
                CookieManagementId = cookieManagement.Id,
                EventType = model.EventType,
                AcceptedCategoriesJson = acceptedCategoriesJson,
                PageUrl = model.PageUrl,
                SessionId = model.SessionId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsMobile = isMobile,
                CreatedAt = DateTime.Now
            };

            _context.CookieConsentEvents.Add(trackingEvent);

            if (model.EventType == "impression")
            {
                cookieManagement.ImpressionCount++;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording cookie tracking event {EventType} for company {CompanyId}", model.EventType, model.CompanyId);
            return false;
        }
    }

    public async Task<CookieManagementStatsViewModel> GetCookieStatsAsync(Guid companyId)
    {
        try
        {
            var cookieManagement = await _context.CookieManagements
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId);

            if (cookieManagement == null)
                return new CookieManagementStatsViewModel();

            var events = await _context.CookieConsentEvents
                .Where(e => e.CookieManagementId == cookieManagement.Id)
                .ToListAsync();

            if (!events.Any())
                return new CookieManagementStatsViewModel();

            var totalImpressions = events.Count(e => e.EventType == "impression");
            var totalAcceptAll = events.Count(e => e.EventType == "accept_all");
            var totalRejectAll = events.Count(e => e.EventType == "reject_all");
            var totalSaveSettings = events.Count(e => e.EventType == "save_settings");

            var totalActions = totalAcceptAll + totalRejectAll + totalSaveSettings;

            var stats = new CookieManagementStatsViewModel
            {
                TotalImpressions = totalImpressions,
                TotalAcceptAll = totalAcceptAll,
                TotalRejectAll = totalRejectAll,
                TotalSaveSettings = totalSaveSettings,
                AcceptRate = totalActions > 0 ? Math.Round((double)(totalAcceptAll + totalSaveSettings) / totalActions * 100, 1) : 0,
                RejectRate = totalActions > 0 ? Math.Round((double)totalRejectAll / totalActions * 100, 1) : 0,
                MobileCount = events.Count(e => e.IsMobile),
                DesktopCount = events.Count(e => !e.IsMobile)
            };

            // Kategori bazlı istatistikler
            var consentEvents = events.Where(e => e.EventType != "impression" && !string.IsNullOrEmpty(e.AcceptedCategoriesJson)).ToList();
            if (consentEvents.Any())
            {
                var categories = cookieManagement.Categories;
                var categoryStats = new List<CookieCategoryStatsViewModel>();

                foreach (var category in categories)
                {
                    var acceptCount = 0;
                    foreach (var evt in consentEvents)
                    {
                        try
                        {
                            var accepted = JsonSerializer.Deserialize<List<string>>(evt.AcceptedCategoriesJson!);
                            if (accepted != null && accepted.Contains(category.Id))
                                acceptCount++;
                        }
                        catch { /* skip malformed JSON */ }
                    }

                    categoryStats.Add(new CookieCategoryStatsViewModel
                    {
                        CategoryId = category.Id,
                        CategoryName = category.Name,
                        AcceptCount = acceptCount,
                        TotalEvents = consentEvents.Count,
                        AcceptRate = consentEvents.Count > 0 ? Math.Round((double)acceptCount / consentEvents.Count * 100, 1) : 0
                    });
                }

                stats.CategoryStats = categoryStats;
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cookie stats for company {CompanyId}", companyId);
            return new CookieManagementStatsViewModel();
        }
    }
}
