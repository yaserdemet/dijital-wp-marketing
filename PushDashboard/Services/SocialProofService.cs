using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PushDashboard.Services;

public interface ISocialProofService
{
    Task<SocialProofSettings?> GetSettingsAsync(Guid companyId);
    Task<SocialProofSettings> GetOrCreateSettingsAsync(Guid companyId, string userId);
    Task<bool> UpdateSettingsAsync(Guid companyId, SocialProofSettings settings, string userId);
    Task<bool> ToggleActiveStatusAsync(Guid companyId, string userId);
    Task<SocialProofScriptConfig> GetScriptConfigAsync(Guid companyId);
    Task<string> ExportSocialProofToR2Async(Guid companyId);
}

public class SocialProofService : ISocialProofService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SocialProofService> _logger;
    private readonly IR2StorageService _r2StorageService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public SocialProofService(
        ApplicationDbContext context,
        ILogger<SocialProofService> logger,
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

    public async Task<SocialProofSettings?> GetSettingsAsync(Guid companyId)
    {
        try
        {
            return await _context.SocialProofSettings
                .Include(sps => sps.Company)
                .Include(sps => sps.UpdatedByUser)
                .FirstOrDefaultAsync(sps => sps.CompanyId == companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting social proof settings for company {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<SocialProofSettings> GetOrCreateSettingsAsync(Guid companyId, string userId)
    {
        try
        {
            var settings = await GetSettingsAsync(companyId);
            
            if (settings == null)
            {
                // Default ayarlarla yeni settings oluştur
                settings = new SocialProofSettings
                {
                    CompanyId = companyId,
                    IsActive = true,
                    IsViewersActive = true,
                    ViewersMin = 15,
                    ViewersMax = 45,
                    IsFollowersActive = true,
                    FollowersMin = 5,
                    FollowersMax = 20,
                    IsBuyersActive = true,
                    BuyersMin = 2,
                    BuyersMax = 8,
                    UpdateInterval = 60,
                    DisplayDuration = 5,
                    UpdatedByUserId = userId,
                    TextTemplates = new SocialProofTextTemplates(),
                    DisplaySettings = new SocialProofDisplaySettings()
                };

                _context.SocialProofSettings.Add(settings);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created default social proof settings for company {CompanyId}", companyId);
            }

            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating social proof settings for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<bool> UpdateSettingsAsync(Guid companyId, SocialProofSettings settings, string userId)
    {
        try
        {
            var existingSettings = await _context.SocialProofSettings
                .FirstOrDefaultAsync(sps => sps.CompanyId == companyId);

            if (existingSettings == null)
            {
                return false;
            }

            // Ayarları güncelle
            existingSettings.IsActive = settings.IsActive;
            existingSettings.IsViewersActive = settings.IsViewersActive;
            existingSettings.ViewersMin = settings.ViewersMin;
            existingSettings.ViewersMax = settings.ViewersMax;
            existingSettings.IsFollowersActive = settings.IsFollowersActive;
            existingSettings.FollowersMin = settings.FollowersMin;
            existingSettings.FollowersMax = settings.FollowersMax;
            existingSettings.IsBuyersActive = settings.IsBuyersActive;
            existingSettings.BuyersMin = settings.BuyersMin;
            existingSettings.BuyersMax = settings.BuyersMax;
            existingSettings.UpdateInterval = settings.UpdateInterval;
            existingSettings.DisplayDuration = settings.DisplayDuration;
            existingSettings.TextTemplates = settings.TextTemplates;
            existingSettings.DisplaySettings = settings.DisplaySettings;
            existingSettings.UpdatedByUserId = userId;
            existingSettings.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated social proof settings for company {CompanyId} by user {UserId}", companyId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating social proof settings for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<bool> ToggleActiveStatusAsync(Guid companyId, string userId)
    {
        try
        {
            var settings = await _context.SocialProofSettings
                .FirstOrDefaultAsync(sps => sps.CompanyId == companyId);

            if (settings == null)
            {
                return false;
            }

            settings.IsActive = !settings.IsActive;
            settings.UpdatedByUserId = userId;
            settings.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Toggled social proof status to {Status} for company {CompanyId}", 
                settings.IsActive ? "Active" : "Inactive", companyId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling social proof status for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<SocialProofScriptConfig> GetScriptConfigAsync(Guid companyId)
    {
        try
        {
            var settings = await GetSettingsAsync(companyId);
            
            if (settings == null || !settings.IsActive)
            {
                return new SocialProofScriptConfig { IsActive = false };
            }

            return new SocialProofScriptConfig
            {
                IsActive = true,
                CompanyId = companyId,
                IsViewersActive = settings.IsViewersActive,
                ViewersMin = settings.ViewersMin,
                ViewersMax = settings.ViewersMax,
                IsFollowersActive = settings.IsFollowersActive,
                FollowersMin = settings.FollowersMin,
                FollowersMax = settings.FollowersMax,
                IsBuyersActive = settings.IsBuyersActive,
                BuyersMin = settings.BuyersMin,
                BuyersMax = settings.BuyersMax,
                UpdateInterval = settings.UpdateInterval * 1000, // JavaScript için milisaniye
                DisplayDuration = settings.DisplayDuration * 1000, // JavaScript için milisaniye
                TextTemplates = settings.TextTemplates,
                DisplaySettings = settings.DisplaySettings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting script config for company {CompanyId}", companyId);
            return new SocialProofScriptConfig { IsActive = false };
        }
    }

    public async Task<string> ExportSocialProofToR2Async(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Starting R2 export for social proof. CompanyId: {CompanyId}", companyId);

            var config = await GetScriptConfigAsync(companyId);
            if (!config.IsActive)
            {
                _logger.LogWarning("Social proof is not active for company {CompanyId}", companyId);
                return "Social proof is not active";
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
            var jsonKey = $"{companyId}/socialProof.json";
            var jsonUrl = await _r2StorageService.UploadJsonAsync(jsonKey, jsonContent);

            // wwwroot/js/modules'dan JS dosyasını oku ve R2'ye yükle
            var jsFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "js", "modules", "social-proof-module.js");
            if (File.Exists(jsFilePath))
            {
                var jsScript = await File.ReadAllTextAsync(jsFilePath);
                var jsKey = $"{companyId}/socialProof.js";
                var jsUrl = await _r2StorageService.UploadJavaScriptAsync(jsKey, jsScript);

                _logger.LogInformation("Successfully exported social proof to R2 for company {CompanyId}. JSON: {JsonUrl}, JS: {JsUrl}",
                    companyId, jsonUrl, jsUrl);
            }
            else
            {
                _logger.LogWarning("Social proof module JS file not found at {Path}", jsFilePath);
            }

            return jsonUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting social proof to R2 for company {CompanyId}", companyId);
            throw;
        }
    }
}

/// <summary>
/// JavaScript widget için konfigürasyon modeli
/// </summary>
public class SocialProofScriptConfig
{
    public bool IsActive { get; set; }
    public Guid CompanyId { get; set; }
    public bool IsViewersActive { get; set; }
    public int ViewersMin { get; set; }
    public int ViewersMax { get; set; }
    public bool IsFollowersActive { get; set; }
    public int FollowersMin { get; set; }
    public int FollowersMax { get; set; }
    public bool IsBuyersActive { get; set; }
    public int BuyersMin { get; set; }
    public int BuyersMax { get; set; }
    public int UpdateInterval { get; set; } // milisaniye
    public int DisplayDuration { get; set; } // milisaniye
    public SocialProofTextTemplates TextTemplates { get; set; } = new();
    public SocialProofDisplaySettings DisplaySettings { get; set; } = new();
}
