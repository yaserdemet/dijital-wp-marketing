using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.Integrations.Common;
using PushDashboard.Services.Integrations.Common.Models;
using System.Text.Json;
using PushDashboard.Services.Notifications;
using PushDashboard.Services;
using PushDashboard.DTOs;

namespace PushDashboard.Services.Modules.FirstOrder;

public class FirstOrderModuleService : IFirstOrderModuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FirstOrderModuleService> _logger;
    private readonly INotificationChannelFactory _channelFactory;
    private readonly EcommerceGiftVoucherFactory _giftVoucherFactory;
    private readonly IEcommerceService _ecommerceService;
    private readonly IModuleUsageService _moduleUsageService;

    public FirstOrderModuleService(
        ApplicationDbContext context,
        ILogger<FirstOrderModuleService> logger,
        INotificationChannelFactory channelFactory,
        EcommerceGiftVoucherFactory giftVoucherFactory,
        IEcommerceService ecommerceService,
        IModuleUsageService moduleUsageService)
    {
        _context = context;
        _logger = logger;
        _channelFactory = channelFactory;
        _giftVoucherFactory = giftVoucherFactory;
        _ecommerceService = ecommerceService;
        _moduleUsageService = moduleUsageService;
    }

    public async Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> SendFirstOrderNotificationAsync(
        Guid companyId, int customerId, string userId)
    {
        try
        {
            _logger.LogInformation("Processing first order notification for customer {CustomerId} in company {CompanyId}",
                customerId, companyId);

            // İlk alışveriş modülünü kontrol et
            var firstOrderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "İlk Alışveriş Mesajı" && m.IsActive);

            if (firstOrderModule == null)
            {
                return (false, "İlk alışveriş mesajı modülü bulunamadı.", 0, 0);
            }

            // Şirketin bu modüle sahip olup olmadığını kontrol et (IsOwned && IsActive && !IsPassive)
            var companyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == firstOrderModule.Id &&
                                         cm.IsOwned &&
                                         cm.IsActive &&
                                         !cm.IsPassive);

            if (companyModule == null)
            {
                return (false, "Şirket bu modüle sahip değil veya modül pasif durumda.", 0, 0);
            }

            // Modül ayarlarını al
            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, firstOrderModule.Id);
            if (moduleSettings == null || !IsModuleEnabled(moduleSettings))
            {
                return (false, "Modül etkin değil.", 0, 0);
            }

            // Müşteri bilgilerini al
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId);

            if (customer == null)
            {
                return (false, "Müşteri bulunamadı.", 0, 0);
            }

            // Şirket bilgilerini al
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
            {
                return (false, "Şirket bulunamadı.", 0, 0);
            }

            // İlk sipariş kontrolü
            var isFirstOrder = await IsFirstOrderAsync(companyId, customerId);
            if (!isFirstOrder)
            {
                return (false, "Bu müşterinin daha önce siparişi var.", 0, 0);
            }

            // Etkin iletişim kanallarını al
            var enabledChannels = await GetEnabledChannelsAsync(companyId, moduleSettings);
            if (!enabledChannels.Any())
            {
                return (false, "Etkin iletişim kanalı bulunamadı.", 0, 0);
            }

            // Toplam maliyet hesaplama
            decimal totalEstimatedCost = 0;
            var channelCosts = new Dictionary<string, decimal>();

            foreach (var channel in enabledChannels)
            {
                var cost = GetChannelCost(channel.ChannelType);
                channelCosts[channel.ChannelType] = cost;
                totalEstimatedCost += cost;
            }

            // Kredi kontrolü
            if (company.CreditBalance < totalEstimatedCost)
            {
                return (false, $"Yetersiz kredi. Gerekli: {totalEstimatedCost:C}, Mevcut: {company.CreditBalance:C}", 0, 0);
            }

            _logger.LogInformation("Sending first order notifications to {Count} channels for customer {CustomerId}",
                enabledChannels.Count, customerId);

            int notificationsSent = 0;
            decimal totalCost = 0;

            // Hediye çeki oluştur (eğer etkinse ve Ticimax entegrasyonu varsa)
            string? giftVoucherCode = null;
            string? giftVoucherAmount = null;

            if (await ShouldCreateGiftVoucherAsync(companyId, moduleSettings))
            {
                var giftVoucherResult = await CreateGiftVoucherAsync(companyId, customer, moduleSettings);
                if (giftVoucherResult.Success)
                {
                    giftVoucherCode = giftVoucherResult.VoucherCode;
                    giftVoucherAmount = giftVoucherResult.Amount;
                }
            }

            var variables = CreateFirstOrderVariables(customer, company, giftVoucherCode, giftVoucherAmount);

            // Her etkin kanal için bildirim gönder
            foreach (var channel in enabledChannels)
            {
                try
                {
                    var cost = channelCosts[channel.ChannelType];
                    var stringVariables = variables.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");
                    var referenceId = $"first_order_{customer.Id}_{DateTime.Now:yyyyMMdd}";
                    var templateName = GetTemplateNameForChannel(moduleSettings, channel.ChannelType);
                    if (string.IsNullOrWhiteSpace(templateName))
                        continue;
                    
                    // Maliyet takibi ile bildirim gönder
                    bool sent = await channel.SendNotificationWithCostTrackingAsync(
                        companyId,
                        customer,
                        templateName,
                        stringVariables,
                        userId,
                        firstOrderModule.Id,
                        referenceId
                    );

                    if (sent)
                    {
                        notificationsSent++;
                        totalCost += cost;
                        _logger.LogInformation("First order notification sent via {ChannelType} to customer {CustomerId}",
                            channel.ChannelType, customerId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send first order notification via {ChannelType} to customer {CustomerId}",
                            channel.ChannelType, customerId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending first order notification via {ChannelType} to customer {CustomerId}",
                        channel.ChannelType, customerId);
                }
            }

            if (notificationsSent > 0)
            {
                return (true, $"{notificationsSent} kanal üzerinden ilk alışveriş mesajı gönderildi.", notificationsSent, totalCost);
            }

            return (false, "Hiçbir kanal üzerinden mesaj gönderilemedi.", 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing first order notification for customer {CustomerId} in company {CompanyId}",
                customerId, companyId);
            return (false, "İlk alışveriş mesajı gönderilirken hata oluştu: " + ex.Message, 0, 0);
        }
    }

    private string? GetTemplateNameForChannel(Dictionary<string, object> moduleSettings, string channelChannelType)
    {
        var key = channelChannelType.ToLower() switch
        {
            "email" => "emailTemplateName",
            "sms" => "smsTemplateName",
            "whatsapp" => "whatsappTemplateName",
            _ => null
        };
        
        if (key == null)
            return null;

        if (moduleSettings.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }

        return null;
    }

    public async Task<bool> IsFirstOrderAsync(Guid companyId, int customerId)
    {
        try
        {
            // Müşteri bilgilerini al
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId);

            if (customer == null || customer.ExternalId <= 0)
            {
                _logger.LogWarning("Customer {CustomerId} not found or has invalid ExternalId", customerId);
                return false;
            }

            // Şirketin Ticimax entegrasyonunu al
            var ticimaxIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "Ticimax" &&
                                         ci.IsActive && ci.IsConfigured);

            if (ticimaxIntegration == null)
            {
                _logger.LogWarning("No active Ticimax integration found for company {CompanyId}", companyId);
                return false;
            }

            var settings = ticimaxIntegration.Settings;
            var baseUrl = settings.GetValueOrDefault("apiUrl", "https://perlucia.ticimaxtest.com").ToString();
            var apiKey = settings.GetValueOrDefault("apiKey", "").ToString();

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Ticimax API key not found for company {CompanyId}", companyId);
                return false;
            }

            // Filtre oluştur - sadece bu müşterinin siparişleri
            var filter = new EcommerceOrderFilter
            {
                CustomerId = customer.ExternalId.ToString(),
                PaymentCompleted = true
            };

            // Sayfalama - sadece 2 kayıt yeterli (varsa ilk sipariş değil)
            var pagination = new EcommercePagination
            {
                PageNumber = 1,
                PageSize = 2,
                SortField = "OrderDate",
                SortDirection = "ASC"
            };

            // E-commerce service ile müşterinin geçmiş siparişlerini sorgula
            var orders = await _ecommerceService.GetOrdersAsync(companyId, filter, pagination);

            // Eğer hiç sipariş yoksa, bu ilk sipariş
            var isFirstOrder = orders != null && orders.Length == 1;

            _logger.LogInformation("Customer {CustomerId} (ExternalId: {ExternalId}) first order check: {IsFirstOrder}. Found {OrderCount} previous orders.",
                customerId, customer.ExternalId, isFirstOrder, orders?.Length ?? 0);

            return isFirstOrder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if customer {CustomerId} has first order", customerId);
            return false; // Hata durumunda güvenli tarafta kal
        }
    }

    private async Task<Dictionary<string, object>?> GetModuleSettingsByIdAsync(Guid companyId, int moduleId)
    {
        try
        {
            // IsOwned kontrolü kullan (kaydetme ile tutarlı olması için)
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.ModuleId == moduleId && cm.IsOwned);

            if (companyModule?.Settings != null)
            {
                _logger.LogDebug("Module settings found for company {CompanyId}, module {ModuleId}: {SettingsCount} settings", 
                    companyId, moduleId, companyModule.Settings.Settings?.Count ?? 0);
                return companyModule.Settings.Settings;
            }

            _logger.LogDebug("No module settings found for company {CompanyId}, module {ModuleId}, checking default settings", 
                companyId, moduleId);

            // Varsayılan ayarları al
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

    private async Task<bool> ShouldCreateGiftVoucherAsync(Guid companyId, Dictionary<string, object> settings)
    {
        try
        {
            // Hediye çeki etkin mi kontrol et - GetBoolSetting metodunu kullan
            var isEnabled = GetBoolSetting(settings, "giftVoucherEnabled", false);

            _logger.LogInformation("Gift voucher enabled check for company {CompanyId}: {IsEnabled}", companyId, isEnabled);

            if (!isEnabled)
            {
                _logger.LogInformation("Gift voucher is disabled for company {CompanyId}", companyId);
                return false;
            }

            // Herhangi bir e-ticaret entegrasyonu var mı ve hediye çeki destekliyor mu kontrol et
            var hasIntegration = await _giftVoucherFactory.HasActiveEcommerceIntegrationAsync(companyId);
            _logger.LogInformation("E-commerce integration check for company {CompanyId}: {HasIntegration}", companyId, hasIntegration);
            
            return hasIntegration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if gift voucher should be created for company {CompanyId}", companyId);
            return false;
        }
    }

    private async Task<(bool Success, string? VoucherCode, string? Amount)> CreateGiftVoucherAsync(
        Guid companyId, Customer customer, Dictionary<string, object> settings)
    {
        try
        {
            // Hediye çeki ayarlarını al
            var giftVoucherSettings = ExtractGiftVoucherSettings(settings);

            // Factory üzerinden hediye çeki oluştur
            var result = await _giftVoucherFactory.CreateGiftVoucherAsync(companyId, customer, giftVoucherSettings);

            if (result.Success)
            {
                _logger.LogInformation("Gift voucher created for first order customer {CustomerId}: {VoucherCode}",
                    customer.Id, result.VoucherCode);
                return (true, result.VoucherCode, result.Amount);
            }
            else
            {
                _logger.LogWarning("Failed to create gift voucher for first order customer {CustomerId}: {Message}",
                    customer.Id, result.ErrorMessage);
                return (false, null, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gift voucher for first order customer {CustomerId}", customer.Id);
            return (false, null, null);
        }
    }

    /// <summary>
    /// İlk alışveriş müşterisi için Ticimax'a kişiye özel hediye çeki oluşturur
    /// - UyeID ile üye bazlı çeki
    /// - Üye adı ile grup adı
    /// - Maksimum kullanım sayısı: 1
    /// </summary>
    private async Task<(bool Success, string? VoucherCode, decimal? Amount, string? ErrorMessage)> CreateGiftVoucherForFirstOrderAsync(
        Guid companyId, int customerExternalId, string customerName, Dictionary<string, object> settings)
    {
        try
        {
            // Hediye çeki ayarlarını al
            var giftVoucherSettings = ExtractGiftVoucherSettings(settings);

            // E-ticaret servisini al
            var ecommerceService = await _giftVoucherFactory.GetActiveEcommerceServiceAsync(companyId);
            if (ecommerceService == null)
            {
                return (false, null, null, "Aktif e-ticaret entegrasyonu bulunamadı.");
            }

            // Kişiye özel hediye çeki isteği oluştur
            var giftVoucherRequest = new EcommerceGiftVoucherRequest
            {
                Name = !string.IsNullOrEmpty(giftVoucherSettings.Description)
                    ? $"{giftVoucherSettings.Description} - {customerName}"
                    : $"İlk Alışveriş Hediye Çeki - {customerName}",
                DiscountType = giftVoucherSettings.DiscountType == 1 ? "AMOUNT" : "PERCENTAGE",
                DiscountValue = (decimal)giftVoucherSettings.Amount,
                CustomerId = customerExternalId.ToString(), // Kişiye özel - UyeID olarak gidecek
                CustomerName = customerName, // Üye adı - Ticimax'ta UyeAdi alanına yazılacak
                ExpiryDate = DateTime.Now.AddDays(giftVoucherSettings.ValidityDays > 0 ? giftVoucherSettings.ValidityDays : 30),
                UsageLimit = 1 // Maksimum kullanım sayısı  1
            };

            _logger.LogInformation("Creating gift voucher for first order customer. CustomerExternalId: {CustomerExternalId}, Name: {CustomerName}, Amount: {Amount}",
                customerExternalId, customerName, giftVoucherSettings.Amount);

            // Ticimax'a hediye çeki gönder
            var result = await ecommerceService.CreateGiftVoucherAsync(companyId, giftVoucherRequest);

            if (result != null && result.Success && !string.IsNullOrEmpty(result.VoucherCode))
            {
                _logger.LogInformation("Gift voucher created successfully for first order customer {CustomerExternalId}: {VoucherCode}",
                    customerExternalId, result.VoucherCode);
                
                return (true, result.VoucherCode, (decimal)giftVoucherSettings.Amount, null);
            }
            else
            {
                var errorMessage = result?.Message ?? "Hediye çeki oluşturulamadı.";
                _logger.LogWarning("Failed to create gift voucher for first order customer {CustomerExternalId}: {Message}",
                    customerExternalId, errorMessage);
                
                return (false, null, null, errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gift voucher for first order customer {CustomerExternalId}", customerExternalId);
            return (false, null, null, ex.Message);
        }
    }

    private GiftVoucherSettings ExtractGiftVoucherSettings(Dictionary<string, object> settings)
    {
        var giftVoucherSettings = new GiftVoucherSettings
        {
            Description = "İlk Alışveriş Hediye Çeki" // İlk alışveriş için özel açıklama
        };

        if (settings.TryGetValue("giftVoucherAmount", out var amountValue))
        {
            if (amountValue is decimal amount)
                giftVoucherSettings.Amount = (double)amount;
            else if (amountValue is JsonElement amountElement && amountElement.TryGetDecimal(out var amountDecimal))
                giftVoucherSettings.Amount = (double)amountDecimal;
        }

        if (settings.TryGetValue("giftVoucherDiscountType", out var discountTypeValue))
        {
            if (discountTypeValue is int discountType)
                giftVoucherSettings.DiscountType = discountType;
            else if (discountTypeValue is JsonElement discountElement && discountElement.TryGetInt32(out var discountInt))
                giftVoucherSettings.DiscountType = discountInt;
        }

        if (settings.TryGetValue("giftVoucherValidityDays", out var validityValue))
        {
            if (validityValue is int validity)
                giftVoucherSettings.ValidityDays = validity;
            else if (validityValue is JsonElement validityElement && validityElement.TryGetInt32(out var validityInt))
                giftVoucherSettings.ValidityDays = validityInt;
        }

        return giftVoucherSettings;
    }

    private Dictionary<string, object> CreateFirstOrderVariables(Customer customer, Company company, string? giftVoucherCode, string? giftVoucherAmount)
    {
        var variables = new Dictionary<string, object>
        {
            ["customer_name"] = $"{customer.FirstName} {customer.LastName}".Trim(),
            ["customer_first_name"] = customer.FirstName ?? "",
            ["customer_last_name"] = customer.LastName ?? "",
            ["customer_email"] = customer.Email ?? "",
            ["customer_phone"] = (customer.Phone ?? customer.MobilePhone) ?? "",
            ["current_date"] = DateTime.Now.ToString("dd.MM.yyyy"),
            ["current_time"] = DateTime.Now.ToString("HH:mm"),
        };

        // Hediye çeki bilgileri varsa ekle
        if (!string.IsNullOrEmpty(giftVoucherCode))
        {
            variables["gift_voucher_code"] = giftVoucherCode;
            variables["gift_voucher_amount"] = giftVoucherAmount ?? "";
            variables["has_gift_voucher"] = true;
        }
        else
        {
            variables["gift_voucher_code"] = "";
            variables["gift_voucher_amount"] = "";
            variables["has_gift_voucher"] = false;
        }

        return variables;
    }

    private async Task<List<INotificationChannelService>> GetEnabledChannelsAsync(Guid companyId, Dictionary<string, object> moduleSettings)
    {
        var enabledChannels = new List<INotificationChannelService>();

        try
        {
            // Email kanalı kontrol et
            if (IsChannelEnabled(moduleSettings, "email"))
            {
                var emailChannel = await _channelFactory.GetChannelAsync(companyId, "email");
                if (emailChannel != null)
                {
                    enabledChannels.Add(emailChannel);
                }
            }

            // WhatsApp kanalı kontrol et
            if (IsChannelEnabled(moduleSettings, "whatsapp"))
            {
                var whatsappChannel = await _channelFactory.GetChannelAsync(companyId, "whatsapp");
                if (whatsappChannel != null)
                {
                    enabledChannels.Add(whatsappChannel);
                }
            }

            // SMS kanalı kontrol et (gelecekte eklenecek)
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
            // Email kanalını kontrol et
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

            // WhatsApp kanalını kontrol et
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

            // SMS kanalını kontrol et
            var smsChannel = await _channelFactory.GetChannelAsync(companyId, "sms");
            if (smsChannel != null)
            {
                var testResult = await smsChannel.TestConnectionAsync(companyId);
                activeChannels.Add(new ActiveChannelDto
                {
                    ChannelType = "sms",
                    DisplayName = "SMS",
                    IsConfigured = true,
                    IsEnabled = true,
                    CostPerMessage = GetChannelCost("sms"),
                    ConfigurationStatus = testResult.Success ? "Yapılandırılmış" : "Hata var",
                    LastTestResult = testResult.Success ? "Başarılı" : testResult.Message,
                    LastTestedAt = DateTime.Now
                });
            }

            // Push notification kanalını kontrol et (gelecekte eklenecek - şimdilik sadece yapılandırılmışsa göster)
            // Push henüz desteklenmediği için şimdilik eklenmeyecek

            _logger.LogInformation("Retrieved {Count} active channels for company {CompanyId}", activeChannels.Count, companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active channels for company {CompanyId}", companyId);
        }

        return activeChannels;
    }

    public async Task<FirstOrderSettingsDto> GetModuleSettingsAsync(Guid companyId)
    {
        try
        {
            // İlk alışveriş modülünü al
            var firstOrderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "İlk Alışveriş Mesajı" && m.IsActive);

            if (firstOrderModule == null)
            {
                throw new InvalidOperationException("İlk alışveriş mesajı modülü bulunamadı.");
            }

            // Modül ayarlarını al
            var moduleSettings = await GetModuleSettingsByIdAsync(companyId, firstOrderModule.Id);
            var settings = moduleSettings ?? new Dictionary<string, object>();

            // Aktif kanalları al
            var activeChannels = await GetActiveChannelsAsync(companyId);

            // E-ticaret entegrasyonu kontrolü
            var hasEcommerceIntegration = await _giftVoucherFactory.HasActiveEcommerceIntegrationAsync(companyId);
            var activePlatform = await _giftVoucherFactory.GetActivePlatformNameAsync(companyId);

            return new FirstOrderSettingsDto
            {
                Enabled = GetBoolSetting(settings, "enabled", false),
                EmailEnabled = GetBoolSetting(settings, "emailEnabled", false),
                SmsEnabled = GetBoolSetting(settings, "smsEnabled", false),
                WhatsAppEnabled = GetBoolSetting(settings, "whatsappEnabled", false),
                PushEnabled = GetBoolSetting(settings, "pushEnabled", false),
                GiftVoucherEnabled = GetBoolSetting(settings, "giftVoucherEnabled", false),
                GiftVoucherAmount = GetDecimalSetting(settings, "giftVoucherAmount", 50),
                GiftVoucherDiscountType = GetIntSetting(settings, "giftVoucherDiscountType", 1),
                GiftVoucherValidityDays = GetIntSetting(settings, "giftVoucherValidityDays", 30),
                AvailableChannels = activeChannels,
                HasActiveEcommerceIntegration = hasEcommerceIntegration,
                ActiveEcommercePlatform = activePlatform,
                // Template bilgileri
                EmailTemplateId = settings.TryGetValue("emailTemplateId", out var emailTplId) && emailTplId != null ? Convert.ToInt32(emailTplId) : (int?)null,
                EmailTemplateName = settings.TryGetValue("emailTemplateName", out var emailTplName) ? emailTplName?.ToString() : null,
                SmsTemplateId = settings.TryGetValue("smsTemplateId", out var smsTplId) && smsTplId != null ? Convert.ToInt32(smsTplId) : (int?)null,
                SmsTemplateName = settings.TryGetValue("smsTemplateName", out var smsTplName) ? smsTplName?.ToString() : null,
                WhatsappTemplateName = settings.TryGetValue("whatsappTemplateName", out var waTplName) ? waTplName?.ToString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module settings for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> UpdateModuleSettingsAsync(Guid companyId, UpdateFirstOrderSettingsRequest request, string userId)
    {
        try
        {
            // İlk alışveriş modülünü al
            var firstOrderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "İlk Alışveriş Mesajı" && m.IsActive);

            if (firstOrderModule == null)
            {
                return (false, "İlk alışveriş mesajı modülü bulunamadı.");
            }

            // Şirketin bu modüle sahip olup olmadığını kontrol et (IsOwned)
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == firstOrderModule.Id &&
                                         cm.IsOwned);

            if (companyModule == null)
            {
                return (false, "Şirket bu modüle sahip değil.");
            }

            // Ayarları hazırla
            var settings = new Dictionary<string, object>
            {
                ["enabled"] = request.Enabled,
                ["emailEnabled"] = request.EmailEnabled,
                ["smsEnabled"] = request.SmsEnabled,
                ["whatsappEnabled"] = request.WhatsAppEnabled,
                ["pushEnabled"] = request.PushEnabled,
                ["giftVoucherEnabled"] = request.GiftVoucherEnabled,
                ["giftVoucherAmount"] = request.GiftVoucherAmount,
                ["giftVoucherDiscountType"] = request.GiftVoucherDiscountType,
                ["giftVoucherValidityDays"] = request.GiftVoucherValidityDays,
                ["updatedAt"] = DateTime.Now,
                ["updatedBy"] = userId
            };

            // Template bilgilerini ekle
            if (request.EmailTemplateId.HasValue)
                settings["emailTemplateId"] = request.EmailTemplateId.Value;
            if (!string.IsNullOrEmpty(request.EmailTemplateName))
                settings["emailTemplateName"] = request.EmailTemplateName;
            
            if (request.SmsTemplateId.HasValue)
                settings["smsTemplateId"] = request.SmsTemplateId.Value;
            if (!string.IsNullOrEmpty(request.SmsTemplateName))
                settings["smsTemplateName"] = request.SmsTemplateName;
            
            if (!string.IsNullOrEmpty(request.WhatsAppTemplateName))
                settings["whatsappTemplateName"] = request.WhatsAppTemplateName;

            // Ayarları kaydet
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

            _logger.LogInformation("First order module settings updated for company {CompanyId} by user {UserId}", companyId, userId);
            return (true, "Ayarlar başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module settings for company {CompanyId}", companyId);
            return (false, "Ayarlar güncellenirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<GiftVoucherTestResult> TestGiftVoucherCreationAsync(Guid companyId)
    {
        try
        {
            // E-ticaret entegrasyonu kontrolü
            var hasIntegration = await _giftVoucherFactory.HasActiveEcommerceIntegrationAsync(companyId);
            if (!hasIntegration)
            {
                return new GiftVoucherTestResult
                {
                    Success = false,
                    ErrorMessage = "Aktif e-ticaret entegrasyonu bulunamadı. Hediye çeki oluşturmak için önce bir e-ticaret platformu entegrasyonu yapılandırın.",
                    Platform = "Yok"
                };
            }

            var platform = await _giftVoucherFactory.GetActivePlatformNameAsync(companyId);

            // Test müşterisi oluştur
            var testCustomer = new Customer
            {
                Id = 0,
                ExternalId = 999999, // Test ID
                FirstName = "Test",
                LastName = "Müşteri",
                Email = "test@example.com",
                CompanyId = companyId
            };

            // Test ayarları
            var testSettings = new GiftVoucherSettings
            {
                Amount = 25,
                DiscountType = 1,
                ValidityDays = 30,
                Description = "Test Hediye Çeki"
            };

            // Hediye çeki oluşturmayı test et
            var result = await _giftVoucherFactory.CreateGiftVoucherAsync(companyId, testCustomer, testSettings);

            return new GiftVoucherTestResult
            {
                Success = result.Success,
                VoucherCode = result.VoucherCode,
                Amount = result.Amount,
                ErrorMessage = result.ErrorMessage,
                Platform = platform ?? "Bilinmiyor"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing gift voucher creation for company {CompanyId}", companyId);
            return new GiftVoucherTestResult
            {
                Success = false,
                ErrorMessage = "Test sırasında hata oluştu: " + ex.Message,
                Platform = "Hata"
            };
        }
    }

    public async Task<FirstOrderStatsDto> GetModuleStatsAsync(Guid companyId)
    {
        try
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // İlk alışveriş modülünü al
            var firstOrderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "İlk Alışveriş Mesajı" && m.IsActive);

            if (firstOrderModule == null)
            {
                return new FirstOrderStatsDto();
            }

            // Modül kullanım loglarını al
            var usageLogs = await _context.ModuleUsageLogs
                .Where(mul => mul.CompanyId == companyId && mul.ModuleId == firstOrderModule.Id)
                .ToListAsync();

            var thisMonthLogs = usageLogs.Where(mul => mul.CreatedAt >= startOfMonth).ToList();

            // Kanal kullanım istatistikleri
            var channelStats = new Dictionary<string, int>();
            foreach (var log in usageLogs)
            {
                if (!string.IsNullOrEmpty(log.Metadata))
                {
                    try
                    {
                        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(log.Metadata);
                        if (metadata != null && metadata.TryGetValue("channelType", out var channelType))
                        {
                            var channel = channelType.ToString() ?? "unknown";
                            channelStats[channel] = channelStats.GetValueOrDefault(channel, 0) + 1;
                        }
                    }
                    catch
                    {
                        // JSON parse hatası, devam et
                    }
                }

                // Eğer metadata'da channelType yoksa, Channel property'sini kullan
                if (!string.IsNullOrEmpty(log.Channel))
                {
                    channelStats[log.Channel] = channelStats.GetValueOrDefault(log.Channel, 0) + 1;
                }
            }
            
            return new FirstOrderStatsDto
            {
                TotalFirstOrderNotifications = usageLogs.Count,
                TotalGiftVouchersCreated = usageLogs.Count(mul => mul.Metadata?.Contains("giftVoucher") == true),
                TotalCostThisMonth = thisMonthLogs.Sum(mul => mul.Cost),
                NotificationsThisMonth = thisMonthLogs.Count,
                ChannelUsageStats = channelStats,
                LastNotificationSent = usageLogs.OrderByDescending(mul => mul.CreatedAt).FirstOrDefault()?.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module stats for company {CompanyId}", companyId);
            return new FirstOrderStatsDto();
        }
    }
    
    // Yardımcı metotlar
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

    private decimal GetDecimalSetting(Dictionary<string, object> settings, string key, decimal defaultValue)
    {
        if (settings.TryGetValue(key, out var value))
        {
            if (value is decimal decimalValue)
                return decimalValue;
            if (value is JsonElement element && element.TryGetDecimal(out var elementDecimal))
                return elementDecimal;
            if (decimal.TryParse(value.ToString(), out var parsedDecimal))
                return parsedDecimal;
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
    
}