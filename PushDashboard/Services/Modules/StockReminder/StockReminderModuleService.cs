using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.Notifications;
using PushDashboard.DTOs;
using System.Text.Json;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.Integrations.Common;
using PushDashboard.Services.Integrations.Common.Models;
using PushDashboard.Services.Integrations.Ticimax;
using PushDashboard.UrunServis;

namespace PushDashboard.Services.Modules.StockReminder;

public class StockReminderModuleService : IStockReminderModuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StockReminderModuleService> _logger;
    private readonly INotificationChannelFactory _channelFactory;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly EcommerceGiftVoucherFactory _giftVoucherFactory;
    private readonly TicimaxService _ticimaxService;

    public StockReminderModuleService(
        ApplicationDbContext context,
        ILogger<StockReminderModuleService> logger,
        INotificationChannelFactory channelFactory,
        IModuleUsageService moduleUsageService,
        EcommerceGiftVoucherFactory giftVoucherFactory,
        TicimaxService ticimaxService)
    {
        _context = context;
        _logger = logger;
        _channelFactory = channelFactory;
        _moduleUsageService = moduleUsageService;
        _giftVoucherFactory = giftVoucherFactory;
        _ticimaxService = ticimaxService;
    }

    public async Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> SendStockReminderNotificationAsync(
        Guid companyId, int productId, int currentStock, string userId)
    {
        try
        {
            _logger.LogInformation("Processing stock reminder notification for product {ProductId} in company {CompanyId}",
                productId, companyId);

            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                return (false, "Stok hatırlatma modülü bulunamadı.", 0, 0);
            }

            var companyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == stockReminderModule.Id &&
                                         cm.IsOwned &&
                                         cm.IsActive &&
                                         !cm.IsPassive);

            if (companyModule == null)
            {
                return (false, "Şirket bu modüle sahip değil veya modül pasif durumda.", 0, 0);
            }

            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, stockReminderModule.Id);
            if (moduleSettings == null || !IsModuleEnabled(moduleSettings))
            {
                return (false, "Modül etkin değil.", 0, 0);
            }

            var stockThreshold = GetIntSetting(moduleSettings, "stockThreshold", 10);
            if (currentStock >= stockThreshold)
            {
                return (false, $"Stok ({currentStock}) eşik değerinin ({stockThreshold}) altında değil.", 0, 0);
            }

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
            {
                return (false, "Şirket bulunamadı.", 0, 0);
            }

            var enabledChannels = await GetEnabledChannelsAsync(companyId, moduleSettings);
            if (!enabledChannels.Any())
            {
                return (false, "Etkin iletişim kanalı bulunamadı.", 0, 0);
            }

            decimal totalEstimatedCost = 0;
            var channelCosts = new Dictionary<string, decimal>();

            foreach (var channel in enabledChannels)
            {
                var cost = GetChannelCost(channel.ChannelType);
                channelCosts[channel.ChannelType] = cost;
                totalEstimatedCost += cost;
            }

            if (company.CreditBalance < totalEstimatedCost)
            {
                return (false, $"Yetersiz kredi. Gerekli: {totalEstimatedCost:C}, Mevcut: {company.CreditBalance:C}", 0, 0);
            }

            _logger.LogInformation("Sending stock reminder notifications to {Count} channels for product {ProductId}",
                enabledChannels.Count, productId);

            int notificationsSent = 0;
            decimal totalCost = 0;
            
            var variables = new Dictionary<string, object>
            {
                ["product_id"] = productId,
                ["stock_count"] = currentStock,
                ["stock_threshold"] = stockThreshold,
                ["company_name"] = company.Name ?? "",
                ["current_date"] = DateTime.Now.ToString("dd.MM.yyyy"),
                ["current_time"] = DateTime.Now.ToString("HH:mm")
            };

            foreach (var channel in enabledChannels)
            {
                try
                {
                    var cost = channelCosts[channel.ChannelType];
                    var stringVariables = variables.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");
                    var referenceId = $"stock_reminder_{productId}_{DateTime.Now:yyyyMMdd}";

                    bool sent = await channel.SendNotificationWithCostTrackingAsync(
                        companyId,
                        null,
                        "Stok Hatırlatma",
                        stringVariables,
                        userId,
                        stockReminderModule.Id,
                        referenceId
                    );

                    if (sent)
                    {
                        notificationsSent++;
                        totalCost += cost;
                        _logger.LogInformation("Stock reminder notification sent via {ChannelType} for product {ProductId}",
                            channel.ChannelType, productId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send stock reminder notification via {ChannelType} for product {ProductId}",
                            channel.ChannelType, productId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending stock reminder notification via {ChannelType} for product {ProductId}",
                        channel.ChannelType, productId);
                }
            }

            if (notificationsSent > 0)
            {
                return (true, $"{notificationsSent} kanal üzerinden stok hatırlatma mesajı gönderildi.", notificationsSent, totalCost);
            }

            return (false, "Hiçbir kanal üzerinden mesaj gönderilemedi.", 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stock reminder notification for product {ProductId} in company {CompanyId}",
                productId, companyId);
            return (false, "Stok hatırlatma mesajı gönderilirken hata oluştu: " + ex.Message, 0, 0);
        }
    }

    public async Task<bool> IsStockBelowThresholdAsync(Guid companyId, int productId, int currentStock)
    {
        try
        {
            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                return false;
            }

            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, stockReminderModule.Id);
            if (moduleSettings == null || !IsModuleEnabled(moduleSettings))
            {
                return false;
            }

            var stockThreshold = GetIntSetting(moduleSettings, "stockThreshold", 10);
            return currentStock < stockThreshold;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock threshold for product {ProductId}", productId);
            return false;
        }
    }

    private async Task<Dictionary<string, object>?> GetModuleSettingsByIdAsync(Guid companyId, int moduleId)
    {
        try
        {
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.ModuleId == moduleId && cm.IsActive);

            if (companyModule?.Settings != null)
            {
                return companyModule.Settings.Settings;
            }

            var module = await _context.Modules.FirstOrDefaultAsync(m => m.Id == moduleId);
            if (module?.DefaultSettings != null)
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(module.DefaultSettings);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module settings for company {CompanyId} and module {ModuleId}", companyId, moduleId);
            return null;
        }
    }

    private bool IsModuleEnabled(Dictionary<string, object> settings)
    {
        if (settings.TryGetValue("enabled", out var enabledValue))
        {
            if (enabledValue is bool enabled)
                return enabled;
            if (enabledValue is JsonElement element && element.ValueKind == JsonValueKind.True)
                return true;
        }
        return false;
    }

    private async Task<List<INotificationChannelService>> GetEnabledChannelsAsync(Guid companyId, Dictionary<string, object> moduleSettings)
    {
        var enabledChannels = new List<INotificationChannelService>();

        try
        {
            if (IsChannelEnabled(moduleSettings, "email"))
            {
                var emailChannel = await _channelFactory.GetChannelAsync(companyId, "email");
                if (emailChannel != null)
                {
                    enabledChannels.Add(emailChannel);
                }
            }

            if (IsChannelEnabled(moduleSettings, "whatsapp"))
            {
                var whatsappChannel = await _channelFactory.GetChannelAsync(companyId, "whatsapp");
                if (whatsappChannel != null)
                {
                    enabledChannels.Add(whatsappChannel);
                }
            }

            if (IsChannelEnabled(moduleSettings, "sms"))
            {
                var smsChannel = await _channelFactory.GetChannelAsync(companyId, "sms");
                if (smsChannel != null)
                {
                    enabledChannels.Add(smsChannel);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enabled channels for company {CompanyId}", companyId);
        }

        return enabledChannels;
    }

    private bool IsChannelEnabled(Dictionary<string, object> settings, string channelType)
    {
        var settingKey = $"{channelType}Enabled";
        if (settings.TryGetValue(settingKey, out var enabledValue))
        {
            if (enabledValue is bool enabled)
                return enabled;
            if (enabledValue is JsonElement element && element.ValueKind == JsonValueKind.True)
                return true;
        }
        return false;
    }

    private decimal GetChannelCost(string channelType)
    {
        return channelType.ToLower() switch
        {
            "email" => 0.10m,
            "sms" => 0.25m,
            "whatsapp" => 0.15m,
            "push" => 0.05m,
            _ => 0.00m
        };
    }

    public async Task<List<ActiveChannelDto>> GetActiveChannelsAsync(Guid companyId)
    {
        var activeChannels = new List<ActiveChannelDto>();

        try
        {
            var emailChannel = await _channelFactory.GetChannelAsync(companyId, "email");
            if (emailChannel != null)
            {
                var testResult = await emailChannel.TestConnectionAsync(companyId);
                activeChannels.Add(new ActiveChannelDto
                {
                    ChannelType = "email",
                    DisplayName = "Email",
                    IsConfigured = true,
                    IsEnabled = true,
                    CostPerMessage = GetChannelCost("email"),
                    ConfigurationStatus = testResult.Success ? "Yapılandırılmış" : "Hata var",
                    LastTestResult = testResult.Success ? "Başarılı" : testResult.Message,
                    LastTestedAt = DateTime.Now
                });
            }

            var whatsappChannel = await _channelFactory.GetChannelAsync(companyId, "whatsapp");
            if (whatsappChannel != null)
            {
                var testResult = await whatsappChannel.TestConnectionAsync(companyId);
                activeChannels.Add(new ActiveChannelDto
                {
                    ChannelType = "whatsapp",
                    DisplayName = "WhatsApp",
                    IsConfigured = true,
                    IsEnabled = true,
                    CostPerMessage = GetChannelCost("whatsapp"),
                    ConfigurationStatus = testResult.Success ? "Yapılandırılmış" : "Hata var",
                    LastTestResult = testResult.Success ? "Başarılı" : testResult.Message,
                    LastTestedAt = DateTime.Now
                });
            }

            _logger.LogInformation("Retrieved {Count} active channels for company {CompanyId}", activeChannels.Count, companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active channels for company {CompanyId}", companyId);
        }

        return activeChannels;
    }

    public async Task<StockReminderSettingsDto> GetModuleSettingsAsync(Guid companyId)
    {
        try
        {
            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                throw new InvalidOperationException("Stok hatırlatma modülü bulunamadı.");
            }

            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, stockReminderModule.Id);
            var settings = moduleSettings ?? new Dictionary<string, object>();

            var activeChannels = await GetActiveChannelsAsync(companyId);

            var hasEcommerceIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .AnyAsync(ci => ci.CompanyId == companyId && ci.IsActive && ci.IsConfigured);
            var activePlatform = await _giftVoucherFactory.GetActivePlatformNameAsync(companyId);

            return new StockReminderSettingsDto
            {
                Enabled = GetBoolSetting(settings, "enabled", false),
                EmailEnabled = GetBoolSetting(settings, "emailEnabled", false),
                SmsEnabled = GetBoolSetting(settings, "smsEnabled", false),
                WhatsAppEnabled = GetBoolSetting(settings, "whatsappEnabled", false),
                PushEnabled = GetBoolSetting(settings, "pushEnabled", false),
                StockThreshold = Math.Max(1, GetIntSetting(settings, "stockThreshold", 1)), // Minimum 1
                AutoNotifyOnStockArrival = GetBoolSetting(settings, "autoNotifyOnStockArrival", true),
                NotificationMessage = GetStringSetting(settings, "notificationMessage",
                    "Merhaba {CustomerName}, {ProductName} ürününün stoğu azalıyor! Son {StockCount} adet kaldı. Hemen sipariş verin!"),
                StockArrivedMessage = GetStringSetting(settings, "stockArrivedMessage",
                    "Merhaba {CustomerName}, beklediğiniz {ProductName} ürünü stoklara girdi! Hemen sipariş verin: {ProductUrl}"),

                EmailTemplate = GetStringSetting(settings, "emailTemplate", ""),
                EmailTemplateName = GetStringSetting(settings, "emailTemplateName", ""),
                SmsTemplate = GetStringSetting(settings, "smsTemplate", ""),
                SmsTemplateName = GetStringSetting(settings, "smsTemplateName", ""),
                WhatsappTemplate = GetStringSetting(settings, "whatsappTemplate", ""),
                WhatsappTemplateName = GetStringSetting(settings, "whatsappTemplateName", ""),
                AvailableChannels = activeChannels,
                HasActiveEcommerceIntegration = hasEcommerceIntegration,
                ActiveEcommercePlatform = activePlatform
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module settings for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> UpdateModuleSettingsAsync(Guid companyId, UpdateStockReminderSettingsRequest request, string userId)
    {
        try
        {
            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                return (false, "Stok hatırlatma modülü bulunamadı.");
            }

            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == stockReminderModule.Id &&
                                         cm.IsOwned);

            if (companyModule == null)
            {
                return (false, "Şirket bu modüle sahip değil.");
            }

            // Stok eşiği minimum 1 olmalı
            var stockThreshold = Math.Max(1, request.StockThreshold);

            var settings = new Dictionary<string, object>
            {
                ["enabled"] = request.Enabled,
                ["emailEnabled"] = request.EmailEnabled,
                ["smsEnabled"] = request.SmsEnabled,
                ["whatsappEnabled"] = request.WhatsAppEnabled,
                ["pushEnabled"] = request.PushEnabled,
                ["stockThreshold"] = stockThreshold,
                ["autoNotifyOnStockArrival"] = request.AutoNotifyOnStockArrival,
                ["notificationMessage"] = request.NotificationMessage,
                ["stockArrivedMessage"] = request.StockArrivedMessage,
                ["emailTemplate"] = request.EmailTemplate,
                ["emailTemplateName"] = request.EmailTemplateName,
                ["smsTemplate"] = request.SmsTemplate,
                ["smsTemplateName"] = request.SmsTemplateName,
                ["whatsappTemplate"] = request.WhatsAppTemplate,
                ["whatsappTemplateName"] = request.WhatsAppTemplateName,
                ["updatedAt"] = DateTime.Now,
                ["updatedBy"] = userId
            };

            if (companyModule.Settings == null)
            {
                companyModule.Settings = new CompanyModuleSettings
                {
                    CompanyModuleId = companyModule.Id,
                    UpdatedByUserId = userId
                };
                _context.CompanyModuleSettings.Add(companyModule.Settings);
            }

            companyModule.Settings.Settings = settings;
            companyModule.Settings.UpdatedByUserId = userId;
            companyModule.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Stock reminder module settings updated for company {CompanyId} by user {UserId}", companyId, userId);
            return (true, "Ayarlar başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module settings for company {CompanyId}", companyId);
            return (false, "Ayarlar güncellenirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<StockReminderStatsDto> GetModuleStatsAsync(Guid companyId)
    {
        try
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                return new StockReminderStatsDto();
            }

            // Modül ayarlarını al
            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, stockReminderModule.Id);
            var stockThreshold = moduleSettings != null ? GetIntSetting(moduleSettings, "stockThreshold", 1) : 1;

            // Kullanım loglarından istatistikler
            var usageLogs = await _context.ModuleUsageLogs
                .Where(mul => mul.CompanyId == companyId && mul.ModuleId == stockReminderModule.Id)
                .ToListAsync();

            var thisMonthLogs = usageLogs.Where(mul => mul.CreatedAt >= startOfMonth).ToList();

            var channelStats = new Dictionary<string, int>();
            foreach (var log in usageLogs)
            {
                if (!string.IsNullOrEmpty(log.Channel))
                {
                    channelStats[log.Channel] = channelStats.GetValueOrDefault(log.Channel, 0) + 1;
                }
            }

            // Bildirim bekleyen toplam abone sayısı (StockReminderWidgetSubmissions tablosundan)
            var totalPendingSubscribers = await _context.StockReminderWidgetSubmissions
                .Where(s => s.CompanyId == companyId && !s.IsNotified)
                .CountAsync();

            // Toplam abone sayısı (tüm aboneler)
            var totalSubscribers = await _context.StockReminderWidgetSubmissions
                .Where(s => s.CompanyId == companyId)
                .CountAsync();

            return new StockReminderStatsDto
            {
                TotalStockReminderNotifications = usageLogs.Count,
                TotalSubscribers = totalSubscribers,
                TotalPendingSubscribers = totalPendingSubscribers,
                TotalCostThisMonth = thisMonthLogs.Sum(mul => mul.Cost),
                NotificationsThisMonth = thisMonthLogs.Count,
                ChannelUsageStats = channelStats,
                LastNotificationSent = usageLogs.OrderByDescending(mul => mul.CreatedAt).FirstOrDefault()?.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module stats for company {CompanyId}", companyId);
            return new StockReminderStatsDto();
        }
    }

    private bool GetBoolSetting(Dictionary<string, object> settings, string key, bool defaultValue)
    {
        if (settings.TryGetValue(key, out var value))
        {
            if (value is bool boolValue)
                return boolValue;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.True)
                return true;
            if (value is JsonElement element2 && element2.ValueKind == JsonValueKind.False)
                return false;
        }
        return defaultValue;
    }

    private int GetIntSetting(Dictionary<string, object> settings, string key, int defaultValue)
    {
        if (settings.TryGetValue(key, out var value))
        {
            if (value is int intValue)
                return intValue;
            if (value is JsonElement element && element.TryGetInt32(out var elementInt))
                return elementInt;
            if (int.TryParse(value.ToString(), out var parsedInt))
                return parsedInt;
        }
        return defaultValue;
    }

    private string GetStringSetting(Dictionary<string, object> settings, string key, string defaultValue)
    {
        if (settings.TryGetValue(key, out var value))
        {
            if (value is string stringValue)
                return stringValue;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? defaultValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// 3 denemeli varyasyon eşleştirme:
    /// 1) StokKodu + UrunKartiID ile ara
    /// 2) Barkod + UrunKartiID ile ara
    /// 3) UrunKartiID'nin tüm varyasyonlarını çek, StokKodu/Barkod ile client-side eşleştir
    /// </summary>
    private async Task<Varyasyon?> FindVariationByAlarmStockCodeAsync(
        UrunServisClient productClient,
        string apiKey,
        string alarmStokKodu,
        int urunKartiId)
    {
        try
        {
            var sayfalama = new UrunSayfalama { BaslangicIndex = 0, KayitSayisi = 100 };
            var ayar = new SelectVaryasyonAyar();

            // ── Adım 1: StokKodu filtresi ──
            try
            {
                var filtre1 = new VaryasyonFiltre
                {
                    StokKodu = alarmStokKodu,
                    UrunKartiID = urunKartiId
                };

                var result1 = await productClient.SelectVaryasyonAsync(apiKey, filtre1, sayfalama, ayar);
                if (result1 != null && result1.Length > 0)
                {
                    _logger.LogDebug("Variation found via StokKodu filter (Step 1) for StokKodu={StokKodu}", alarmStokKodu);
                    return result1[0];
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Step 1 (StokKodu filter) failed for StokKodu={StokKodu}, UrunKartiID={UrunKartiID}. Trying next step...",
                    alarmStokKodu, urunKartiId);
            }

            // ── Adım 2: Barkod filtresi ──
            try
            {
                var filtre2 = new VaryasyonFiltre
                {
                    Barkod = alarmStokKodu,
                    UrunKartiID = urunKartiId
                };

                var result2 = await productClient.SelectVaryasyonAsync(apiKey, filtre2, sayfalama, ayar);
                if (result2 != null && result2.Length > 0)
                {
                    _logger.LogDebug("Variation found via Barkod filter (Step 2) for StokKodu={StokKodu}", alarmStokKodu);
                    return result2[0];
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Step 2 (Barkod filter) failed for StokKodu={StokKodu}, UrunKartiID={UrunKartiID}. Trying next step...",
                    alarmStokKodu, urunKartiId);
            }

            // ── Adım 3: UrunKartiID ile tüm varyasyonları çek, client-side eşleştir ──
            try
            {
                var filtre3 = new VaryasyonFiltre
                {
                    UrunKartiID = urunKartiId
                };

                var result3 = await productClient.SelectVaryasyonAsync(apiKey, filtre3, sayfalama, ayar);
                if (result3 != null && result3.Length > 0)
                {
                    // Önce StokKodu ile eşleştir
                    var matchByStokKodu = result3.FirstOrDefault(v =>
                        !string.IsNullOrEmpty(v.StokKodu) &&
                        v.StokKodu.Equals(alarmStokKodu, StringComparison.OrdinalIgnoreCase));

                    if (matchByStokKodu != null)
                    {
                        _logger.LogDebug("Variation found via UrunKartiID + StokKodu match (Step 3a) for StokKodu={StokKodu}", alarmStokKodu);
                        return matchByStokKodu;
                    }

                    // Sonra Barkod ile eşleştir
                    var matchByBarkod = result3.FirstOrDefault(v =>
                        !string.IsNullOrEmpty(v.Barkod) &&
                        v.Barkod.Equals(alarmStokKodu, StringComparison.OrdinalIgnoreCase));

                    if (matchByBarkod != null)
                    {
                        _logger.LogDebug("Variation found via UrunKartiID + Barkod match (Step 3b) for StokKodu={StokKodu}", alarmStokKodu);
                        return matchByBarkod;
                    }

                    _logger.LogDebug("UrunKartiID {UrunKartiID} has {Count} variations but none matched StokKodu={StokKodu}",
                        urunKartiId, result3.Length, alarmStokKodu);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Step 3 (UrunKartiID fallback) failed for StokKodu={StokKodu}, UrunKartiID={UrunKartiID}",
                    alarmStokKodu, urunKartiId);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in FindVariationByAlarmStockCodeAsync for StokKodu={StokKodu}, UrunKartiID={UrunKartiID}",
                alarmStokKodu, urunKartiId);
            return null;
        }
    }

    /// <summary>
    /// Stok güncellendiğinde webhook ile tetiklenir - abone müşterilere bildirim gönderir
    /// </summary>
    public async Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> ProcessStockUpdatedWebhookAsync(
        Guid companyId,
        int productId,
        string productName,
        string? productUrl,
        int oldStock,
        int newStock)
    {
        try
        {
            _logger.LogInformation(
                "Processing stock updated webhook for product {ProductId} in company {CompanyId}: {OldStock} -> {NewStock}",
                productId, companyId, oldStock, newStock);

            // Modül kontrolü
            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                return (false, "Stok hatırlatma modülü bulunamadı.", 0, 0);
            }

            var companyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == stockReminderModule.Id &&
                                         cm.IsOwned &&
                                         cm.IsActive &&
                                         !cm.IsPassive);

            if (companyModule == null)
            {
                return (false, "Şirket bu modüle sahip değil.", 0, 0);
            }

            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, stockReminderModule.Id);
            if (moduleSettings == null || !IsModuleEnabled(moduleSettings))
            {
                return (false, "Modül etkin değil.", 0, 0);
            }

            // Otomatik bildirim aktif mi?
            var autoNotify = GetBoolSetting(moduleSettings, "autoNotifyOnStockArrival", true);
            if (!autoNotify)
            {
                return (false, "Otomatik stok bildirimi devre dışı.", 0, 0);
            }

            // Stok eşiği kontrolü - mevcut StockThreshold ayarını kullan
            var stockThreshold = GetIntSetting(moduleSettings, "stockThreshold", 1);

            // Stok geldi mi kontrolü: 
            // - Eski stok eşiğin altında (0, 1, 2 vs.)
            // - Yeni stok eşik veya üstünde
            // Örnek: Eşik=3 ise, stok 0->3 veya 2->5 olduğunda tetiklenir
            if (oldStock >= stockThreshold || newStock < stockThreshold)
            {
                _logger.LogInformation(
                    "Stock update does not meet notification criteria. OldStock: {OldStock}, NewStock: {NewStock}, Threshold: {Threshold}",
                    oldStock, newStock, stockThreshold);
                return (false, "Stok değişimi bildirim kriterlerini karşılamıyor.", 0, 0);
            }

            _logger.LogInformation(
                "Stock arrived! Sending notifications for product {ProductId}. Old: {OldStock}, New: {NewStock}, Threshold: {Threshold}",
                productId, oldStock, newStock, stockThreshold);

            // Stok geldi bildirimi gönder
            return await SendStockArrivedNotificationsAsync(companyId, productId, productName, productUrl, newStock, "system");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stock updated webhook for product {ProductId} in company {CompanyId}",
                productId, companyId);
            return (false, "Stok güncelleme webhook'u işlenirken hata oluştu: " + ex.Message, 0, 0);
        }
    }

    /// <summary>
    /// Belirli bir ürün için stok alarm abonelerine bildirim gönderir
    /// </summary>
    public async Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> SendStockArrivedNotificationsAsync(
        Guid companyId,
        int productId,
        string productName,
        string? productUrl,
        int currentStock,
        string userId)
    {
        try
        {
            _logger.LogInformation("Sending stock arrived notifications for product {ProductId} in company {CompanyId}",
                productId, companyId);

            // Modül kontrolü
            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                return (false, "Stok hatırlatma modülü bulunamadı.", 0, 0);
            }

            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, stockReminderModule.Id);
            if (moduleSettings == null)
            {
                return (false, "Modül ayarları bulunamadı.", 0, 0);
            }

            // Stok alarm abonelerini al (Ticimax'tan)
            var subscribers = await GetStockAlarmSubscribersAsync(companyId, productId);

            if (!subscribers.Any())
            {
                _logger.LogInformation("No stock alarm subscribers found for product {ProductId}", productId);
                return (true, "Bu ürün için stok alarm abonesi bulunamadı.", 0, 0);
            }

            _logger.LogInformation("Found {Count} subscribers for product {ProductId}", subscribers.Count, productId);

            // Etkin kanalları al
            var enabledChannels = await GetEnabledChannelsAsync(companyId, moduleSettings);
            if (!enabledChannels.Any())
            {
                return (false, "Etkin iletişim kanalı bulunamadı.", 0, 0);
            }

            int totalNotificationsSent = 0;
            decimal totalCost = 0;

            foreach (var subscriber in subscribers)
            {
                // Müşteriyi bul - Ticimax aboneleri için Customer tablosundan,
                // Widget aboneleri için doğrudan subscriber bilgilerinden
                Customer? customer = null;
                bool isWidgetSubscriber = subscriber.Source == "widget";

                if (!isWidgetSubscriber)
                {
                    if (subscriber.CustomerId.HasValue)
                    {
                        customer = await _context.Customers.FindAsync(subscriber.CustomerId.Value);
                    }
                    else if (subscriber.ExternalMemberId > 0)
                    {
                        customer = await _context.Customers
                            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.ExternalId == subscriber.ExternalMemberId);
                    }

                    if (customer == null)
                    {
                        _logger.LogWarning("Customer not found for Ticimax subscriber {ExternalMemberId}", subscriber.ExternalMemberId);
                        continue;
                    }
                }

                // Değişkenleri oluştur
                var subscriberName = customer?.FullName ?? subscriber.CustomerName ?? "";
                var subscriberEmail = customer?.Email ?? subscriber.Email ?? "";
                var subscriberPhone = customer?.Phone ?? customer?.MobilePhone ?? subscriber.Phone ?? "";

                var variables = new Dictionary<string, string>
                {
                    ["customer_name"] = subscriberName,
                    ["customer_first_name"] = customer?.FirstName ?? subscriberName.Split(' ').FirstOrDefault() ?? "",
                    ["customer_last_name"] = customer?.LastName ?? (subscriberName.Split(' ').Length > 1 ? string.Join(" ", subscriberName.Split(' ').Skip(1)) : ""),
                    ["customer_email"] = subscriberEmail,
                    ["customer_phone"] = subscriberPhone,
                    ["current_date"] = DateTime.Now.ToString("dd.MM.yyyy"),
                    ["current_time"] = DateTime.Now.ToString("HH:mm"),
                    ["product_name"] = productName,
                    ["product_url"] = productUrl ?? "",
                };

                // Widget abonesi için kanal bilgisi yoksa, doğrudan email/phone üzerinden gönder
                var subscriberIdentifier = isWidgetSubscriber
                    ? $"widget_{subscriber.Email ?? subscriber.Phone ?? Guid.NewGuid().ToString("N")[..8]}"
                    : subscriber.ExternalMemberId.ToString();

                foreach (var channel in enabledChannels)
                {
                    try
                    {
                        var templateName = GetTemplateNameForChannel(moduleSettings, channel.ChannelType);
                        if (string.IsNullOrEmpty(templateName))
                            continue;

                        // Widget abonesi ise kanalın gerektirdiği bilgi var mı kontrol et
                        if (isWidgetSubscriber)
                        {
                            if (channel.ChannelType.Equals("whatsapp", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(subscriberPhone))
                                continue;
                            if (channel.ChannelType.Equals("sms", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(subscriberPhone))
                                continue;
                            if (channel.ChannelType.Equals("email", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(subscriberEmail))
                                continue;
                        }

                        var referenceId = $"stock_arrived_{productId}_{subscriberIdentifier}_{DateTime.Now:yyyyMMddHHmm}";

                        if (customer != null)
                        {
                            // Ticimax abonesi - Customer nesnesi ile gönder
                            bool sent = await channel.SendNotificationWithCostTrackingAsync(
                                companyId,
                                customer,
                                templateName,
                                variables,
                                userId,
                                stockReminderModule.Id,
                                referenceId
                            );

                            if (sent)
                            {
                                totalNotificationsSent++;
                                totalCost += GetChannelCost(channel.ChannelType);
                                _logger.LogInformation(
                                    "Stock arrived notification sent via {ChannelType} to customer {CustomerId} for product {ProductId}",
                                    channel.ChannelType, customer.Id, productId);
                            }
                        }
                        else
                        {
                            // Widget abonesi - Customer nesnesi olmadan, geçici bir Customer oluştur
                            var tempCustomer = new Customer
                            {
                                CompanyId = companyId,
                                FirstName = variables["customer_first_name"],
                                LastName = variables["customer_last_name"],
                                Email = subscriberEmail,
                                Phone = subscriberPhone,
                                MobilePhone = subscriberPhone,
                                SmsPermission = true,
                                IsActive = true
                            };

                            bool sent = await channel.SendNotificationWithCostTrackingAsync(
                                companyId,
                                tempCustomer,
                                templateName,
                                variables,
                                userId,
                                stockReminderModule.Id,
                                referenceId
                            );

                            if (sent)
                            {
                                totalNotificationsSent++;
                                totalCost += GetChannelCost(channel.ChannelType);
                                _logger.LogInformation(
                                    "Stock arrived notification sent via {ChannelType} to widget subscriber {Email}/{Phone} for product {ProductId}",
                                    channel.ChannelType, subscriberEmail, subscriberPhone, productId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error sending stock arrived notification via {ChannelType} to subscriber {Identifier}",
                            channel.ChannelType, subscriberIdentifier);
                    }
                }
            }

            if (totalNotificationsSent > 0)
            {
                return (true, $"{totalNotificationsSent} aboneye stok geldi bildirimi gönderildi.", totalNotificationsSent, totalCost);
            }

            return (false, "Hiçbir aboneye bildirim gönderilemedi.", 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending stock arrived notifications for product {ProductId} in company {CompanyId}",
                productId, companyId);
            return (false, "Stok geldi bildirimi gönderilirken hata oluştu: " + ex.Message, 0, 0);
        }
    }
    
    private string? GetTemplateNameForChannel(Dictionary<string, object> settings, string channelType)
    {
        var key = channelType.ToLower() switch
        {
            "email" => "emailTemplateName",
            "sms" => "smsTemplateName",
            "whatsapp" => "whatsappTemplateName",
            _ => null
        };

        if (key == null)
            return null;

        if (settings.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }

        return null;
    }

    /// <summary>
    /// Ticimax'tan stok alarm abonelerini getirir
    /// </summary>
    public async Task<List<StockAlarmSubscriberDto>> GetStockAlarmSubscribersAsync(Guid companyId, int productId)
    {
        var subscribers = new List<StockAlarmSubscriberDto>();
        // Widget üzerinden gelen aboneleri de ekle
        try
        {
            var productIdStr = productId.ToString();
            var widgetSubmissions = await _context.StockReminderWidgetSubmissions
                .Where(s => s.CompanyId == companyId && s.ProductId == productIdStr)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            foreach (var submission in widgetSubmissions)
            {
                var displayName = !string.IsNullOrWhiteSpace(submission.FirstName) || !string.IsNullOrWhiteSpace(submission.LastName)
                    ? $"{submission.FirstName} {submission.LastName}".Trim()
                    : (!string.IsNullOrEmpty(submission.Phone)
                        ? submission.Phone
                        : (!string.IsNullOrEmpty(submission.Email) ? submission.Email : "Widget Abonesi"));

                subscribers.Add(new StockAlarmSubscriberDto
                {
                    ExternalMemberId = 0,
                    CustomerId = null,
                    CustomerName = displayName,
                    Email = submission.Email,
                    Phone = submission.Phone,
                    SubscribedAt = submission.CreatedAt,
                    Source = "widget",
                    SelectedVariant = submission.SelectedVariant,
                    PageUrl = submission.PageUrl,
                    IsNotified = submission.IsNotified
                });
            }

            if (widgetSubmissions.Any())
            {
                _logger.LogInformation("Found {Count} widget subscribers for product {ProductId}", widgetSubmissions.Count, productId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting widget subscribers for product {ProductId} in company {CompanyId}",
                productId, companyId);
        }

        return subscribers;
    }

    /// <summary>
    /// E-ticaret sisteminden stok alarm ürünlerini çeker ve stok durumlarını döndürür
    /// </summary>
    public async Task<StockReminderPullResult> PullStockRemindersAsync(Guid companyId, int productCardId = 0)
    {
        try
        {
            _logger.LogInformation("Pulling stock reminder products for company {CompanyId}, productCardId: {ProductCardId}",
                companyId, productCardId);

            // Modül kontrolü
            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                return new StockReminderPullResult { Success = false, Message = "Stok hatırlatma modülü bulunamadı." };
            }

            var companyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == stockReminderModule.Id &&
                                         cm.IsOwned &&
                                         cm.IsActive &&
                                         !cm.IsPassive);

            if (companyModule == null)
            {
                return new StockReminderPullResult { Success = false, Message = "Şirket bu modüle sahip değil veya modül pasif durumda." };
            }

            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, stockReminderModule.Id);
            if (moduleSettings == null || !IsModuleEnabled(moduleSettings))
            {
                return new StockReminderPullResult { Success = false, Message = "Modül etkin değil." };
            }

            var stockThreshold = GetIntSetting(moduleSettings, "stockThreshold", 1);

            // Ticimax entegrasyonu
            var integration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId
                    && ci.Integration.Name == "Ticimax"
                    && ci.IsActive
                    && ci.IsConfigured);

            if (integration == null || string.IsNullOrEmpty(integration.SettingsJson))
            {
                return new StockReminderPullResult { Success = false, Message = "Ticimax entegrasyonu bulunamadı veya yapılandırılmamış." };
            }

            string apiUrl = "";
            string apiKey = "";

            using var doc = JsonDocument.Parse(integration.SettingsJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("apiUrl", out var urlProp))
                apiUrl = urlProp.GetString() ?? "";
            if (root.TryGetProperty("apiKey", out var keyProp))
                apiKey = keyProp.GetString() ?? "";

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
            {
                return new StockReminderPullResult { Success = false, Message = "Ticimax API bilgileri eksik." };
            }

            var client = _ticimaxService.CreateCustomServiceClient(apiUrl);
            var productClient = _ticimaxService.CreateProductServiceClient(apiUrl);
            var request = new CustomServis.GetStokAlarmUrunlerRequest
            {
                UrunKartiId = productCardId,
                UyeID = 0
            };

            var response = await client.GetStokAlarmUrunlerAsync(apiKey, request);
            
            if (response.IsError)
            {
                return new StockReminderPullResult { Success = false, Message = $"Ticimax API hatası: {response.ErrorMessage}" };
            }

            if (response.Urunler == null || response.Urunler.Length == 0)
            {
                return new StockReminderPullResult
                {
                    Success = true,
                    Message = "Stok alarm kaydı bulunamadı.",
                    TotalProductsWithAlarm = 0
                };
            }

            // Müşteri bilgilerini çek
            var memberIds = response.Urunler.Select(u => u.UyeID).Distinct().ToList();
            var customers = await _context.Customers
                .Where(c => c.CompanyId == companyId && memberIds.Contains(c.ExternalId))
                .ToDictionaryAsync(c => c.ExternalId, c => c);

            // Mevcut DB kayıtlarını çek (upsert için) — sadece ticimax kaynağından gelenler
            var existingItems = await _context.StockReminderWidgetSubmissions
                .Where(s => s.CompanyId == companyId)
                .ToListAsync();

            var existingMap = existingItems
                .Where(s => !string.IsNullOrWhiteSpace(s.ProductId))
                .ToDictionary(s => s.ProductId, s => s);

            var now = DateTime.UtcNow;
            var processedAlarmIds = new HashSet<int>();

            var ticimaxVariations = new List<Varyasyon>();
            foreach (var items in response.Urunler.Select(u => u.StokAlarmUrunID).Distinct().Chunk(50))
            {
                var variations = await productClient.SelectVaryasyonAsync(apiKey, new VaryasyonFiltre() { UrunIdList = items, Aktif = -1 }, new UrunSayfalama { BaslangicIndex = 0, KayitSayisi = items.Count() }, new SelectVaryasyonAyar());
                ticimaxVariations.AddRange(variations.ToList());
            }
            
            // Ticimax verilerini DB'ye kaydet/güncelle
            foreach (var urun in response.Urunler)
            {
                var customer = customers.GetValueOrDefault(urun.UyeID);

                var variations = ticimaxVariations.FirstOrDefault(v => v.ID == urun.StokAlarmUrunID);
                string variantName = string.Empty;
                if (variations != null && variations?.Ozellikler != null && variations.Ozellikler.Length > 0)
                    variantName = string.Join(" - ", variations.Ozellikler.Select(o => o.Deger));
                
                if (existingMap.TryGetValue(urun.StokAlarmUrunID.ToString(), out var existingItem))
                {
                    // Güncelle
                    existingItem.ProductId = urun.StokAlarmUrunID.ToString();
                    existingItem.ProductName = urun.UrunAdi + " - " + variantName ?? $"Ürün #{urun.UrunKartiID}";
                    existingItem.StockCode = urun.StokKodu;
                    existingItem.TotalStock = (int)urun.ToplamStokAdedi;
                    existingItem.Price = (decimal)urun.UrunFiyati;
                    existingItem.PriceWithTax = (decimal)urun.UrunFiyatiKdv;
                    existingItem.Currency = urun.ParaBirimi;
                    existingItem.ImageUrl = urun.ResimUrl;
                    existingItem.ProductUrl = urun.UrunUrl;
                    existingItem.ExternalMemberId = urun.UyeID;
                    existingItem.FirstName = customer?.FullName ?? $"Üye #{urun.UyeID}";
                    existingItem.Email = customer?.Email;
                    existingItem.Phone = customer?.Phone ?? customer?.MobilePhone;
                    existingItem.LastSyncedAt = now;
                }
                else
                {
                    // Yeni kayıt ekle
                    var newItem = new StockReminderWidgetSubmission
                    {
                        CompanyId = companyId,
                        Source = "ticimax",
                        ExternalAlarmId = urun.StokAlarmUrunID,
                        ExternalMemberId = urun.UyeID,
                        ProductId = urun.StokAlarmUrunID.ToString(),
                        ProductName = urun.UrunAdi  + " - " + variantName ?? $"Ürün #{urun.UrunKartiID}",
                        StockCode = urun.StokKodu,
                        TotalStock = (int)urun.ToplamStokAdedi,
                        Price = (decimal)urun.UrunFiyati,
                        PriceWithTax = (decimal)urun.UrunFiyatiKdv,
                        Currency = urun.ParaBirimi,
                        ImageUrl = urun.ResimUrl,
                        ProductUrl = urun.UrunUrl,
                        FirstName = customer?.FullName ?? $"Üye #{urun.UyeID}",
                        Email = customer?.Email,
                        Phone = customer?.Phone ?? customer?.MobilePhone,
                        CreatedAt = urun.EklemeTarihi,
                        LastSyncedAt = now,
                        IsNotified = false
                    };
                    _context.StockReminderWidgetSubmissions.Add(newItem);
                }

                processedAlarmIds.Add(urun.StokAlarmUrunID);
            }

            // Ticimax'ta artık olmayan ticimax kayıtlarını sil
            var removedItems = existingItems
                .Where(e => e.ExternalAlarmId > 0 && !processedAlarmIds.Contains(e.ExternalAlarmId))
                .ToList();
            if (removedItems.Any())
            {
                _context.StockReminderWidgetSubmissions.RemoveRange(removedItems);
                _logger.LogInformation("Removed {Count} stale stock reminder items for company {CompanyId}",
                    removedItems.Count, companyId);
            }

            await _context.SaveChangesAsync();

            // Widget kaynağından gelen kayıtların ürün bilgilerini güncelle
            try
            {
                var widgetSubmissions = await _context.StockReminderWidgetSubmissions
                    .Where(s => s.CompanyId == companyId &&
                               s.Source == "widget" &&
                               s.ProductId != null &&
                               !s.IsNotified)
                    .ToListAsync();

                if (widgetSubmissions.Any())
                {
                    // Benzersiz ürün ID'lerini al
                    var distinctProductIds = widgetSubmissions
                        .Select(s => s.ProductId!)
                        .Distinct()
                        .ToList();

                    _logger.LogInformation(
                        "Refreshing product data for {Count} widget submissions ({ProductCount} distinct products) in company {CompanyId}",
                        widgetSubmissions.Count, distinctProductIds.Count, companyId);

                    // Her ürün için güncel bilgiyi çek ve cache'le
                    var productCache = new Dictionary<string, EcommerceProduct?>();
                    foreach (var pid in distinctProductIds)
                    {
                        try
                        {
                            var product = await _ticimaxService.GetProductAsync(companyId, pid);
                            productCache[pid] = product;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to fetch product {ProductId} for widget submission refresh", pid);
                            productCache[pid] = null;
                        }
                    }

                    // Widget submission'larını güncelle
                    foreach (var sub in widgetSubmissions)
                    {
                        if (productCache.TryGetValue(sub.ProductId!, out var product) && product != null)
                        {
                            sub.ProductName = product.Name ?? sub.ProductName ?? $"Ürün #{sub.ProductId}";
                            sub.StockCode = product.ProductCode ?? sub.StockCode;
                            sub.TotalStock = product.StockQuantity;
                            sub.Price = product.Price;
                            sub.PriceWithTax = product.KDVAmount.HasValue
                                ? product.Price + product.KDVAmount.Value
                                : product.Price;
                            sub.Currency = product.Currency ?? sub.Currency;
                            sub.ImageUrl = product.MainImage ?? sub.ImageUrl;
                            sub.ProductUrl = product.ProductUrl ?? sub.ProductUrl;
                            sub.LastSyncedAt = now;
                        }
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation(
                        "Refreshed product data for {Count} widget submissions in company {CompanyId}",
                        widgetSubmissions.Count, companyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing widget submission product data for company {CompanyId}", companyId);
                // Ticimax sync zaten başarılı oldu, widget refresh hatası ana akışı bozmasın
            }

            return new StockReminderPullResult{ Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling stock reminders for company {CompanyId}", companyId);
            return new StockReminderPullResult { Success = false, Message = "Stok alarm ürünleri çekilirken hata oluştu: " + ex.Message };
        }
    }

    /// <summary>
    /// Ürün servisinden belirli bir ürünün güncel bilgilerini alarak, o ürüne ait stok hatırlatma kayıtlarını günceller.
    /// </summary>
    public async Task<StockReminderPullResult> RefreshProductStockDataAsync(Guid companyId, int productId)
    {
        try
        {
            _logger.LogInformation("Refreshing product data for company {CompanyId}, productId: {ProductId}",
                companyId, productId);

            string productIdStr = productId.ToString();

            // Sadece bu ürüne ait abonelikleri çek (kayıt yoksa işlem yapmaya gerek yok)
            var submissions = await _context.StockReminderWidgetSubmissions
                .Where(s => s.CompanyId == companyId && s.ProductId == productIdStr)
                .ToListAsync();

            if (!submissions.Any())
            {
                return new StockReminderPullResult 
                { 
                    Success = true, 
                    Message = "Bu ürün için bekleyen stok hatırlatma kaydı bulunmamaktadır.",
                    TotalProductsWithAlarm = 0 
                };
            }

            // E-ticaret servisinden sadece bu ürünü çek
            var product = await _ticimaxService.GetVariationAsync(companyId, productIdStr);
            if (product == null)
            {
                return new StockReminderPullResult { Success = false, Message = "E-ticaret sisteminden ürün bilgisi alınamadı." };
            }

            var productCart = await _ticimaxService.GetProductAsync(companyId, product.ExternalId);
            if (productCart == null)
            {
                return new StockReminderPullResult { Success = false, Message = "E-ticaret sisteminden ürün bilgisi alınamadı." };
            }
            
            var now = DateTime.UtcNow;

            // Kayıtları güncelle
            foreach (var sub in submissions)
            {
                sub.ProductName = productCart.Name + " - " + product.Name;
                sub.StockCode =  sub.StockCode;
                sub.TotalStock = product.StockQuantity;
                sub.Price = product.Price;
                sub.PriceWithTax = product.Price;
                sub.Currency = productCart.Currency ?? sub.Currency;
                sub.ImageUrl = sub.ImageUrl;
                sub.ProductUrl = sub.ProductUrl;
                sub.LastSyncedAt = now;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully refreshed data for product {ProductId}, updated {Count} submissions in company {CompanyId}",
                productId, submissions.Count, companyId);

            return new StockReminderPullResult { Success = true, Message = "Ürün bilgileri ve stok başarıyla güncellendi." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing product data for productId {ProductId} in company {CompanyId}", productId, companyId);
            return new StockReminderPullResult { Success = false, Message = "Ürün verisi güncellenirken hata oluştu: " + ex.Message };
        }
    }

    /// <summary>
    /// Stoğu gelen ürünlerin abonelerine kanallar üzerinden mesaj gönderir (StockReminderWidgetSubmissions tablosundan okur)
    /// </summary>
    public async Task<StockReminderSendResult> SendStockReminderMessagesAsync(Guid companyId, List<int>? productIds = null)
    {
        try
        {
            _logger.LogInformation("Sending stock reminder messages for company {CompanyId}, productIds: {ProductIds}",
                companyId, productIds != null ? string.Join(",", productIds) : "all");

            // Modül kontrolü
            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                return new StockReminderSendResult { Success = false, Message = "Stok hatırlatma modülü bulunamadı." };
            }

            var companyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == stockReminderModule.Id &&
                                         cm.IsOwned &&
                                         cm.IsActive &&
                                         !cm.IsPassive);

            if (companyModule == null)
            {
                return new StockReminderSendResult { Success = false, Message = "Şirket bu modüle sahip değil veya modül pasif durumda." };
            }

            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, stockReminderModule.Id);
            if (moduleSettings == null || !IsModuleEnabled(moduleSettings))
            {
                return new StockReminderSendResult { Success = false, Message = "Modül etkin değil." };
            }

            var stockThreshold = GetIntSetting(moduleSettings, "stockThreshold", 1);

            // StockReminderWidgetSubmissions tablosundan oku
            var query = _context.StockReminderWidgetSubmissions
                .Where(s => s.CompanyId == companyId &&
                           s.TotalStock >= stockThreshold &&
                           !s.IsNotified);

            // Belirli ürün ID'leri verilmişse filtrele
            if (productIds != null && productIds.Any())
            {
                var productIdStrings = productIds.Select(p => p.ToString()).ToList();
                query = query.Where(s => s.ProductId != null && productIdStrings.Contains(s.ProductId));
            }

            // Ürünleri grupla (aynı ürünün birden fazla abonesi olabilir)
            var dbItems = await query.ToListAsync();

            var productGroups = dbItems
                .Where(s => s.ProductId != null)
                .GroupBy(s => s.ProductId!)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName = g.First().ProductName ?? $"Ürün #{g.Key}",
                    ProductUrl = g.First().ProductUrl,
                    CurrentStock = g.First().TotalStock,
                    SubscriberCount = g.Count(),
                    Items = g.ToList()
                })
                .ToList();

            if (!productGroups.Any())
            {
                return new StockReminderSendResult
                {
                    Success = true,
                    Message = "Bildirim gönderilecek stoğu gelen ürün bulunamadı.",
                    TotalProducts = 0
                };
            }

            var sendResult = new StockReminderSendResult
            {
                Success = true,
                TotalProducts = productGroups.Count
            };

            foreach (var product in productGroups)
            {
                try
                {
                    if (!int.TryParse(product.ProductId, out var productIdInt))
                    {
                        _logger.LogWarning("Invalid product ID format: {ProductId}", product.ProductId);
                        continue;
                    }

                    var (success, message, notificationsSent, totalCost) = await SendStockArrivedNotificationsAsync(
                        companyId,
                        productIdInt,
                        product.ProductName,
                        product.ProductUrl,
                        product.CurrentStock,
                        "system");

                    sendResult.ProductResults.Add(new ProductNotificationResult
                    {
                        ProductId = productIdInt,
                        ProductName = product.ProductName,
                        NotificationsSent = notificationsSent,
                        Cost = totalCost,
                        Error = success ? null : message
                    });

                    sendResult.TotalNotificationsSent += notificationsSent;
                    sendResult.TotalCost += totalCost;

                    // Bildirimi gönderilen kayıtları işaretle
                    if (success && notificationsSent > 0)
                    {
                        var now = DateTime.UtcNow;
                        foreach (var item in product.Items)
                        {
                            item.IsNotified = true;
                            item.NotifiedAt = now;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending stock reminder messages for product {ProductId}", product.ProductId);
                    sendResult.ProductResults.Add(new ProductNotificationResult
                    {
                        ProductId = int.TryParse(product.ProductId, out var pid) ? pid : 0,
                        ProductName = product.ProductName,
                        NotificationsSent = 0,
                        Error = ex.Message
                    });
                }
            }

            // Bildirim durumlarını kaydet
            await _context.SaveChangesAsync();

            sendResult.Message = $"{sendResult.TotalProducts} ürün için {sendResult.TotalNotificationsSent} bildirim gönderildi. Toplam maliyet: {sendResult.TotalCost:C}";

            _logger.LogInformation(
                "Stock reminder messages sent for company {CompanyId}: {Products} products, {Notifications} notifications, {Cost} cost",
                companyId, sendResult.TotalProducts, sendResult.TotalNotificationsSent, sendResult.TotalCost);

            return sendResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending stock reminder messages for company {CompanyId}", companyId);
            return new StockReminderSendResult { Success = false, Message = "Stok hatırlatma mesajları gönderilirken hata oluştu: " + ex.Message };
        }
    }

    /// <summary>
    /// Tüm şirketler için stok alarm ürünlerini çeker
    /// </summary>
    public async Task<AllCompaniesStockReminderPullResult> PullAllStockRemindersAsync()
    {
        var allResult = new AllCompaniesStockReminderPullResult { Success = true };

        try
        {
            _logger.LogInformation("Pulling stock reminders for all companies");

            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                return new AllCompaniesStockReminderPullResult { Success = false, Message = "Stok hatırlatma modülü bulunamadı." };
            }

            var companyModules = await _context.CompanyModules
                .Include(cm => cm.Company)
                .Where(cm => cm.ModuleId == stockReminderModule.Id &&
                           cm.IsOwned &&
                           cm.IsActive &&
                           !cm.IsPassive)
                .ToListAsync();

            allResult.TotalCompaniesProcessed = companyModules.Count;

            foreach (var cm in companyModules)
            {
                try
                {
                    var result = await PullStockRemindersAsync(cm.CompanyId);
                    allResult.CompanyResults.Add(new CompanyStockReminderPullResult
                    {
                        CompanyId = cm.CompanyId,
                        CompanyName = cm.Company?.Name,
                        Result = result
                    });

                    if (result.Success)
                        allResult.TotalCompaniesSucceeded++;
                    else
                        allResult.TotalCompaniesFailed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error pulling stock reminders for company {CompanyId}", cm.CompanyId);
                    allResult.CompanyResults.Add(new CompanyStockReminderPullResult
                    {
                        CompanyId = cm.CompanyId,
                        CompanyName = cm.Company?.Name,
                        Error = ex.Message
                    });
                    allResult.TotalCompaniesFailed++;
                }
            }

            allResult.Message = $"{allResult.TotalCompaniesProcessed} şirket işlendi. {allResult.TotalCompaniesSucceeded} başarılı, {allResult.TotalCompaniesFailed} başarısız.";

            _logger.LogInformation("Pull all stock reminders completed: {Processed} companies, {Succeeded} succeeded, {Failed} failed",
                allResult.TotalCompaniesProcessed, allResult.TotalCompaniesSucceeded, allResult.TotalCompaniesFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PullAllStockRemindersAsync");
            allResult.Success = false;
            allResult.Message = "Tüm şirketler için stok alarm ürünleri çekilirken hata oluştu: " + ex.Message;
        }

        return allResult;
    }

    /// <summary>
    /// Tüm şirketler için stoğu gelen ürünlerin abonelerine mesaj gönderir
    /// </summary>
    public async Task<AllCompaniesStockReminderSendResult> SendAllStockReminderMessagesAsync()
    {
        var allResult = new AllCompaniesStockReminderSendResult { Success = true };

        try
        {
            _logger.LogInformation("Sending stock reminder messages for all companies");

            var stockReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive);

            if (stockReminderModule == null)
            {
                return new AllCompaniesStockReminderSendResult { Success = false, Message = "Stok hatırlatma modülü bulunamadı." };
            }

            var companyModules = await _context.CompanyModules
                .Include(cm => cm.Company)
                .Where(cm => cm.ModuleId == stockReminderModule.Id &&
                           cm.IsOwned &&
                           cm.IsActive &&
                           !cm.IsPassive)
                .ToListAsync();

            allResult.TotalCompaniesProcessed = companyModules.Count;

            foreach (var cm in companyModules)
            {
                try
                {
                    var result = await SendStockReminderMessagesAsync(cm.CompanyId);
                    allResult.CompanyResults.Add(new CompanyStockReminderSendResult
                    {
                        CompanyId = cm.CompanyId,
                        CompanyName = cm.Company?.Name,
                        Result = result
                    });

                    allResult.TotalNotificationsSent += result.TotalNotificationsSent;
                    allResult.TotalCost += result.TotalCost;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending stock reminder messages for company {CompanyId}", cm.CompanyId);
                    allResult.CompanyResults.Add(new CompanyStockReminderSendResult
                    {
                        CompanyId = cm.CompanyId,
                        CompanyName = cm.Company?.Name,
                        Error = ex.Message
                    });
                }
            }

            allResult.Message = $"{allResult.TotalCompaniesProcessed} şirket işlendi. Toplam {allResult.TotalNotificationsSent} bildirim gönderildi. Maliyet: {allResult.TotalCost:C}";

            _logger.LogInformation("Send all stock reminder messages completed: {Companies} companies, {Notifications} notifications, {Cost} cost",
                allResult.TotalCompaniesProcessed, allResult.TotalNotificationsSent, allResult.TotalCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendAllStockReminderMessagesAsync");
            allResult.Success = false;
            allResult.Message = "Tüm şirketler için stok hatırlatma mesajları gönderilirken hata oluştu: " + ex.Message;
        }

        return allResult;
    }
}
