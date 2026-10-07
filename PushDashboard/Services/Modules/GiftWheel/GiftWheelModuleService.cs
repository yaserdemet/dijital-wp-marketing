using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Services.Modules.GiftWheel;

/// <summary>
/// Service for Gift Wheel module management (following existing module patterns)
/// </summary>
public class GiftWheelModuleService : IGiftWheelModuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GiftWheelModuleService> _logger;

    public GiftWheelModuleService(
        ApplicationDbContext context,
        ILogger<GiftWheelModuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Module Settings

    public async Task<GiftWheelModuleSettingsDto> GetModuleSettingsAsync(Guid companyId)
    {
        try
        {
            // Get Gift Wheel module
            var giftWheelModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Hediye Çarkı" && m.IsActive);

            if (giftWheelModule == null)
            {
                return new GiftWheelModuleSettingsDto();
            }

            // Get company module settings
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == giftWheelModule.Id &&
                                         cm.IsActive);

            if (companyModule?.Settings != null)
            {
                var settings = companyModule.Settings.Settings;
                return new GiftWheelModuleSettingsDto
                {
                    IsEnabled = GetSettingValue<bool>(settings, "enabled", true),
                    NotificationCost = GetSettingValue<decimal>(settings, "notificationCost", 1.00m),
                    MaxSpinsPerHour = GetSettingValue<int>(settings, "maxSpinsPerHour", 100),
                    EnableRateLimiting = GetSettingValue<bool>(settings, "enableRateLimiting", true),
                    CompanyWebsiteUrl = GetSettingValue<string>(settings, "companyWebsiteUrl", null)
                };
            }

            // Return default settings from module
            if (!string.IsNullOrEmpty(giftWheelModule.DefaultSettings))
            {
                var defaultSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(giftWheelModule.DefaultSettings);
                if (defaultSettings != null)
                {
                    return new GiftWheelModuleSettingsDto
                    {
                        IsEnabled = GetSettingValue<bool>(defaultSettings, "enabled", true),
                        NotificationCost = GetSettingValue<decimal>(defaultSettings, "notificationCost", 1.00m),
                        MaxSpinsPerHour = GetSettingValue<int>(defaultSettings, "maxSpinsPerHour", 100),
                        EnableRateLimiting = GetSettingValue<bool>(defaultSettings, "enableRateLimiting", true),
                        CompanyWebsiteUrl = GetSettingValue<string>(defaultSettings, "companyWebsiteUrl", null)
                    };
                }
            }

            return new GiftWheelModuleSettingsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module settings for company {CompanyId}", companyId);
            return new GiftWheelModuleSettingsDto();
        }
    }

    public async Task<(bool Success, string Message)> UpdateModuleSettingsAsync(Guid companyId, GiftWheelModuleSettingsDto settings, string userId)
    {
        try
        {
            // Get Gift Wheel module
            var giftWheelModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Hediye Çarkı" && m.IsActive);

            if (giftWheelModule == null)
            {
                return (false, "Hediye çarkı modülü bulunamadı.");
            }

            // Get or create company module
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == giftWheelModule.Id &&
                                         cm.IsActive);

            if (companyModule == null)
            {
                return (false, "Şirket bu modüle sahip değil.");
            }

            // Prepare settings dictionary
            var settingsDict = new Dictionary<string, object>
            {
                ["enabled"] = settings.IsEnabled,
                ["notificationCost"] = settings.NotificationCost,
                ["maxSpinsPerHour"] = settings.MaxSpinsPerHour,
                ["enableRateLimiting"] = settings.EnableRateLimiting,
                ["companyWebsiteUrl"] = settings.CompanyWebsiteUrl ?? ""
            };

            // Update or create settings
            if (companyModule.Settings == null)
            {
                var newSettings = new CompanyModuleSettings
                {
                    CompanyModuleId = companyModule.Id,
                    SettingsJson = JsonSerializer.Serialize(settingsDict),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UpdatedByUserId = userId
                };

                _context.CompanyModuleSettings.Add(newSettings);
            }
            else
            {
                companyModule.Settings.SettingsJson = JsonSerializer.Serialize(settingsDict);
                companyModule.Settings.UpdatedAt = DateTime.Now;
                companyModule.Settings.UpdatedByUserId = userId;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Gift wheel module settings updated for company {CompanyId} by user {UserId}", companyId, userId);
            return (true, "Modül ayarları başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module settings for company {CompanyId}", companyId);
            return (false, "Ayarlar güncellenirken hata oluştu.");
        }
    }

    #endregion

    #region Module Statistics

    public async Task<GiftWheelModuleStatsDto> GetModuleStatsAsync(Guid companyId)
    {
        try
        {
            var wheel = await _context.GiftWheels
                .FirstOrDefaultAsync(w => w.CompanyId == companyId);

            if (wheel == null)
            {
                return new GiftWheelModuleStatsDto();
            }

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var allSpins = await _context.GiftWheelSpins
                .Where(s => s.GiftWheelId == wheel.Id)
                .ToListAsync();

            var recentSpins = allSpins.Where(s => s.SpinDate >= thirtyDaysAgo).ToList();

            var stats = new GiftWheelModuleStatsDto
            {
                ActiveWheels = await _context.GiftWheels.CountAsync(w => w.CompanyId == companyId && w.IsActive),
                TotalSpinsAllTime = allSpins.Count,
                TotalVouchersCreated = allSpins.Count(s => s.VoucherCreated),
                TotalCostAllTime = allSpins.Sum(s => s.Cost ?? 0),
                SpinsLast30Days = recentSpins.Count,
                CostLast30Days = recentSpins.Sum(s => s.Cost ?? 0),
                AverageSpinsPerDay = recentSpins.Count > 0 ? (decimal)recentSpins.Count / 30 : 0,
                ConversionRate = allSpins.Count > 0 ? (decimal)allSpins.Count(s => s.VoucherCreated) / allSpins.Count * 100 : 0
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module stats for company {CompanyId}", companyId);
            return new GiftWheelModuleStatsDto();
        }
    }

    public async Task<List<ModuleUsageDto>> GetModuleUsageHistoryAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
    {
        try
        {
            // Get Gift Wheel module
            var giftWheelModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Hediye Çarkı" && m.IsActive);

            if (giftWheelModule == null)
            {
                return new List<ModuleUsageDto>();
            }

            var query = _context.ModuleUsageLogs
                .Where(mul => mul.CompanyId == companyId && mul.ModuleId == giftWheelModule.Id);

            if (startDate.HasValue)
                query = query.Where(mul => mul.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(mul => mul.CreatedAt <= endDate.Value);

            var usageLogs = await query
                .OrderByDescending(mul => mul.CreatedAt)
                .Take(limit)
                .Select(mul => new ModuleUsageDto
                {
                    Id = mul.Id,
                    UsageType = mul.UsageType,
                    Description = mul.Description,
                    Cost = mul.Cost,
                    IsSuccessful = mul.IsSuccessful,
                    CreatedAt = mul.CreatedAt,
                    ReferenceId = mul.ReferenceId,
                    ErrorMessage = mul.ErrorMessage
                })
                .ToListAsync();

            return usageLogs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module usage history for company {CompanyId}", companyId);
            return new List<ModuleUsageDto>();
        }
    }

    #endregion

    #region Module Status

    public async Task<bool> HasActiveModuleAsync(Guid companyId)
    {
        try
        {
            var giftWheelModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Hediye Çarkı" && m.IsActive);

            if (giftWheelModule == null)
            {
                return false;
            }

            // Sahiplik (IsOwned), aktiflik (IsActive) ve pasif olmama (!IsPassive) kontrolü
            return await _context.CompanyModules
                .AnyAsync(cm => cm.CompanyId == companyId &&
                               cm.ModuleId == giftWheelModule.Id &&
                               cm.IsOwned &&
                               cm.IsActive &&
                               !cm.IsPassive &&
                               (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking active module for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<ModuleStatusDto> GetModuleStatusAsync(Guid companyId)
    {
        try
        {
            var giftWheelModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Hediye Çarkı" && m.IsActive);

            if (giftWheelModule == null)
            {
                return new ModuleStatusDto { IsActive = false };
            }

            // Sahiplik kontrolü için IsOwned kullan
            var companyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == giftWheelModule.Id &&
                                         cm.IsOwned);

            if (companyModule == null)
            {
                return new ModuleStatusDto { IsActive = false };
            }

            return new ModuleStatusDto
            {
                IsActive = companyModule.IsActive && (companyModule.ExpiresAt == null || companyModule.ExpiresAt > DateTime.UtcNow),
                IsConfigured = true, // Gift wheel is configured if it exists
                PurchasedAt = companyModule.PurchasedAt,
                ExpiresAt = companyModule.ExpiresAt,
                PaidAmount = companyModule.PaidAmount,
                TransactionId = companyModule.TransactionId,
                LastUsedAt = companyModule.LastUsedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module status for company {CompanyId}", companyId);
            return new ModuleStatusDto { IsActive = false };
        }
    }

    public async Task<(bool CanUse, string Message, decimal RequiredCredits)> ValidateModuleUsageAsync(Guid companyId, string operationType)
    {
        try
        {
            // Check if module is active
            if (!await HasActiveModuleAsync(companyId))
            {
                return (false, "Hediye çarkı modülü aktif değil.", 0);
            }

            // Get module settings to determine costs
            var settings = await GetModuleSettingsAsync(companyId);
            if (!settings.IsEnabled)
            {
                return (false, "Hediye çarkı modülü devre dışı.", 0);
            }

            // Calculate required credits based on operation type
            decimal requiredCredits = operationType.ToLower() switch
            {
                "notification" => settings.NotificationCost,
                "spin" => settings.NotificationCost,
                _ => 1.00m
            };

            // Check company credits
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
            {
                return (false, "Şirket bilgisi bulunamadı.", requiredCredits);
            }

            if (company.CreditBalance < requiredCredits)
            {
                return (false, $"Yetersiz kredi bakiyesi. Gerekli: {requiredCredits:C}, Mevcut: {company.CreditBalance:C}", requiredCredits);
            }

            return (true, "Kullanım izni verildi.", requiredCredits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating module usage for company {CompanyId}", companyId);
            return (false, "Doğrulama sırasında hata oluştu.", 0);
        }
    }

    #endregion

    #region Integration Checks

    public async Task<IntegrationStatusDto> CheckRequiredIntegrationsAsync(Guid companyId)
    {
        try
        {
            var status = new IntegrationStatusDto();

            // Check e-commerce integration
            var ecommerceIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Type == "ecommerce" &&
                                         ci.IsActive);

            status.HasEcommerceIntegration = ecommerceIntegration != null;
            status.EcommercePlatform = ecommerceIntegration?.Integration.Name;
            status.IsEcommerceConfigured = ecommerceIntegration?.IsConfigured ?? false;

            // Check WhatsApp integration
            var whatsappIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "WhatsApp" &&
                                         ci.IsActive);

            status.HasWhatsAppIntegration = whatsappIntegration != null;
            status.IsWhatsAppConfigured = whatsappIntegration?.IsConfigured ?? false;

            // Determine missing integrations
            if (!status.HasEcommerceIntegration)
            {
                status.MissingIntegrations.Add("E-ticaret platformu (hediye çeki oluşturma için)");
            }
            else if (!status.IsEcommerceConfigured)
            {
                status.ConfigurationIssues.Add("E-ticaret entegrasyonu yapılandırılmamış");
            }

            if (!status.HasWhatsAppIntegration)
            {
                status.MissingIntegrations.Add("WhatsApp (bildirim gönderme için)");
            }
            else if (!status.IsWhatsAppConfigured)
            {
                status.ConfigurationIssues.Add("WhatsApp entegrasyonu yapılandırılmamış");
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking required integrations for company {CompanyId}", companyId);
            return new IntegrationStatusDto();
        }
    }

    public async Task<string?> GetActiveEcommercePlatformAsync(Guid companyId)
    {
        try
        {
            var ecommerceIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Type == "ecommerce" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            return ecommerceIntegration?.Integration.Name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active e-commerce platform for company {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<bool> IsWhatsAppConfiguredAsync(Guid companyId)
    {
        try
        {
            return await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .AnyAsync(ci => ci.CompanyId == companyId &&
                               ci.Integration.Name == "WhatsApp" &&
                               ci.IsActive &&
                               ci.IsConfigured);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking WhatsApp configuration for company {CompanyId}", companyId);
            return false;
        }
    }

    #endregion

    #region Helper Methods

    private T GetSettingValue<T>(Dictionary<string, object> settings, string key, T defaultValue)
    {
        if (settings.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
                }
                return (T)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    #endregion
}
