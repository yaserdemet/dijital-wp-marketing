using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.Integrations.Common.Models;
using PushDashboard.Services.Notifications;
using System.Text.Json;

namespace PushDashboard.Services.Modules.Birthday;

public class BirthdayModuleService : IBirthdayModuleService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationChannelFactory _notificationChannelFactory;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BirthdayModuleService> _logger;
    private readonly EcommerceGiftVoucherFactory _giftVoucherFactory;


    public BirthdayModuleService(
        ApplicationDbContext context,
        INotificationChannelFactory notificationChannelFactory,
        IModuleUsageService moduleUsageService,
        UserManager<ApplicationUser> userManager,
        ILogger<BirthdayModuleService> logger,
        EcommerceGiftVoucherFactory giftVoucherFactory)
    {
        _context = context;
        _notificationChannelFactory = notificationChannelFactory;
        _moduleUsageService = moduleUsageService;
        _userManager = userManager;
        _logger = logger;
        _giftVoucherFactory = giftVoucherFactory;
    }

    public async Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> SendBirthdayNotificationsAsync(Guid companyId, string userId)
    {
        try
        {
            _logger.LogInformation("Starting birthday notification process for company {CompanyId}", companyId);

            var birthdayModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Doğum Günü Hatırlatma" && m.IsActive);

            if (birthdayModule == null)
            {
                return (false, "Doğum günü hatırlatma modülü bulunamadı.", 0, 0);
            }

            var companyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == birthdayModule.Id &&
                                         cm.IsOwned &&
                                         cm.IsActive &&
                                         !cm.IsPassive);

            if (companyModule == null)
            {
                return (false, "Şirket bu modüle sahip değil veya modül pasif durumda.", 0, 0);
            }

            var settings = await GetModuleSettingsAsync(companyId);
            if (settings == null || !IsModuleEnabled(settings))
            {
                return (false, "Modül etkin değil.", 0, 0);
            }

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
            {
                return (false, "Şirket bulunamadı.", 0, 0);
            }

            var today = DateTime.Now.Date;
            var todayMonth = today.Month;
            var todayDay = today.Day;
            var start = today.Date;
            var end = start.AddDays(1);

            var allCustomers = await _context.Customers
                .Where(c => c.CompanyId == companyId &&
                           c.IsActive &&
                           c.BirthDate.HasValue)
                .ToListAsync();

            var birthdayCustomers = allCustomers
                .Select(c => new { Customer = c, LocalBirthDate = c.BirthDate!.Value })
                .Where(x => x.LocalBirthDate.Month == todayMonth && x.LocalBirthDate.Day == todayDay)
                .GroupBy(x => x.Customer.Email.ToLowerInvariant())
                .Select(g => g.OrderByDescending(x => x.Customer.CreatedAt).First())
                .Select(x => x.Customer)
                .ToList();

            if (!birthdayCustomers.Any())
            {
                return (true, "Bugün doğum günü olan müşteri bulunamadı.", 0, 0);
            }

            var enabledChannels = GetEnabledChannels(settings);
            if (!enabledChannels.Any())
            {
                return (false, "Etkin iletişim kanalı bulunamadı.", 0, 0);
            }

            int notificationsSent = 0;
            decimal totalCost = 0;

            foreach (var customer in birthdayCustomers)
            {
                var birthYear = customer.BirthDate!.Value.Year;
                var currentYear = DateTime.Now.Year;

                string? giftVoucherCode = null;
                string? giftVoucherAmount = null;

                foreach (var channelType in enabledChannels)
                {
                    var alreadySent = await _context.BirthdayNotifications
                        .AnyAsync(bn => bn.CompanyId == companyId &&
                                      bn.CustomerId == customer.Id &&
                                      bn.Channel == channelType &&
                                      bn.BirthYear == currentYear &&
                                      bn.Success);

                    if (alreadySent)
                    {
                        _logger.LogInformation("Birthday notification already sent to customer {CustomerId} via {Channel} for year {Year}",
                            customer.Id, channelType, currentYear);
                        continue;
                    }
                    
                    if (await ShouldCreateGiftVoucherAsync(companyId, settings) && string.IsNullOrWhiteSpace(giftVoucherCode))
                    {
                        var giftVoucherResult = await CreateGiftVoucherAsync(companyId, customer, settings);
                        if (giftVoucherResult.Success)
                        {
                            giftVoucherCode = giftVoucherResult.VoucherCode;
                            giftVoucherAmount = giftVoucherResult.Amount;
                        }
                    }

                    var templateName = GetTemplateNameForChannel(settings, channelType);
                    if (string.IsNullOrEmpty(templateName))
                    {
                        _logger.LogWarning("Template not found for channel {Channel} in company {CompanyId}", channelType, companyId);
                        continue;
                    }

                    var variables = CreateBirthdayVariables(customer, company, giftVoucherCode, giftVoucherAmount);

                    try
                    {
                        var channel = await _notificationChannelFactory.GetChannelAsync(companyId, channelType.ToLower());
                        if (channel == null)
                        {
                            _logger.LogWarning("Channel {Channel} not available for company {CompanyId}", channelType, companyId);
                            await LogNotificationAsync(companyId, customer.Id, currentYear, channelType, templateName, false, $"Kanal bulunamadı: {channelType}", 0, userId, false);
                            continue;
                        }

                        var cost = await _moduleUsageService.GetModuleUsageCostAsync(companyId, birthdayModule.Id, channelType.ToLower());

                        if (company.CreditBalance < cost)
                        {
                            _logger.LogWarning("Insufficient credit for company {CompanyId}. Required: {Cost}, Available: {Balance}",
                                companyId, cost, company.CreditBalance);
                            await LogNotificationAsync(companyId, customer.Id, currentYear, channelType, templateName, false, "Yetersiz kredi", cost, userId, false);
                            continue;
                        }

                        var stringVariables = variables.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? "");
                        var referenceId = $"birthday_{customer.Id}_{currentYear}";

                        var sent = await channel.SendNotificationWithCostTrackingAsync(
                            companyId,
                            customer,
                            templateName,
                            stringVariables,
                            userId,
                            birthdayModule.Id,
                            referenceId
                        );

                        if (sent)
                        {
                            notificationsSent++;
                            totalCost += cost;
                            await LogNotificationAsync(companyId, customer.Id, currentYear, channelType, templateName, true, null, cost, userId, false);
                            _logger.LogInformation("Birthday notification sent successfully to customer {CustomerId} via {Channel}",
                                customer.Id, channelType);
                        }
                        else
                        {
                            await LogNotificationAsync(companyId, customer.Id, currentYear, channelType, templateName, false, "Gönderim başarısız", cost, userId, false);
                            _logger.LogWarning("Failed to send birthday notification to customer {CustomerId} via {Channel}",
                                customer.Id, channelType);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending birthday notification to customer {CustomerId} via {Channel}",
                            customer.Id, channelType);
                        await LogNotificationAsync(companyId, customer.Id, currentYear, channelType, templateName, false, ex.Message, 0, userId, false);
                    }
                }
            }

            var message = $"{notificationsSent} doğum günü bildirimi başarıyla gönderildi.";
            return (true, message, notificationsSent, totalCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendBirthdayNotificationsAsync for company {CompanyId}", companyId);
            return (false, $"Hata oluştu: {ex.Message}", 0, 0);
        }
    }

    public async Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> SendBirthdayNotificationsForAllCompaniesAsync()
    {
        try
        {
            _logger.LogInformation("Starting birthday notification process for all companies");

            var birthdayModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Doğum Günü Hatırlatma" && m.IsActive);

            if (birthdayModule == null)
            {
                return (false, "Doğum günü hatırlatma modülü bulunamadı.", 0, 0);
            }

            var companiesWithModule = await _context.CompanyModules
                .Where(cm => cm.ModuleId == birthdayModule.Id &&
                           cm.IsOwned &&
                           cm.IsActive &&
                           !cm.IsPassive)
                .Select(cm => cm.CompanyId)
                .ToListAsync();

            if (!companiesWithModule.Any())
            {
                return (true, "Doğum günü hatırlatma modülüne sahip aktif şirket bulunamadı.", 0, 0);
            }

            int totalNotificationsSent = 0;
            decimal totalCost = 0;
            var errors = new List<string>();

            foreach (var companyId in companiesWithModule)
            {
                try
                {
                    var settings = await GetModuleSettingsAsync(companyId);
                    if (settings == null || !IsModuleEnabled(settings))
                    {
                        continue;
                    }

                    var autoSend = GetBoolSetting(settings, "autoSend", false);
                    if (!autoSend)
                    {
                        _logger.LogInformation("Auto send is disabled for company {CompanyId}, skipping", companyId);
                        continue;
                    }

                    var result = await SendBirthdayNotificationsAsync(companyId, "30eb145f-1cbf-4250-8efa-829f617fac72");
                    if (result.Success)
                    {
                        totalNotificationsSent += result.NotificationsSent;
                        totalCost += result.TotalCost;
                    }
                    else
                    {
                        errors.Add($"Şirket {companyId}: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing birthday notifications for company {CompanyId}", companyId);
                    errors.Add($"Şirket {companyId}: {ex.Message}");
                }
            }

            var message = $"{totalNotificationsSent} doğum günü bildirimi gönderildi.";
            if (errors.Any())
            {
                message += $" {errors.Count} hata oluştu.";
            }

            return (true, message, totalNotificationsSent, totalCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendBirthdayNotificationsForAllCompaniesAsync");
            return (false, $"Hata oluştu: {ex.Message}", 0, 0);
        }
    }

    private async Task<Dictionary<string, object>?> GetModuleSettingsAsync(Guid companyId)
    {
        try
        {
            var birthdayModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Doğum Günü Hatırlatma" && m.IsActive);

            if (birthdayModule == null)
            {
                return null;
            }

            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .Include(cm => cm.Module)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.ModuleId == birthdayModule.Id && cm.IsOwned);

            if (companyModule == null)
            {
                return null;
            }

            if (companyModule?.Settings != null)
            {
                return companyModule.Settings.Settings;
            }

            if (companyModule?.Module?.DefaultSettings != null)
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(companyModule.Module.DefaultSettings);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module settings for company {CompanyId}", companyId);
            return null;
        }
    }

    private bool IsModuleEnabled(Dictionary<string, object> settings)
    {
        return GetBoolSetting(settings, "enabled", true);
    }

    private bool GetBoolSetting(Dictionary<string, object> settings, string key, bool defaultValue)
    {
        if (settings.TryGetValue(key, out var value))
        {
            if (value is bool boolValue)
                return boolValue;
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
                return true;
            if (value is JsonElement jsonElementFalse && jsonElementFalse.ValueKind == JsonValueKind.False)
                return false;
            if (bool.TryParse(value?.ToString(), out var parsed))
                return parsed;
        }
        return defaultValue;
    }

    private List<string> GetEnabledChannels(Dictionary<string, object> settings)
    {
        var channels = new List<string>();

        if (GetBoolSetting(settings, "enableEmail", false))
            channels.Add("Email");

        if (GetBoolSetting(settings, "enableSms", false))
            channels.Add("SMS");

        if (GetBoolSetting(settings, "enableWhatsapp", false))
            channels.Add("WhatsApp");

        return channels;
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

    private Dictionary<string, object> CreateBirthdayVariables(Customer customer, Company company, string? giftVoucherCode = null, string? giftVoucherAmount = null)
    {
        var age = customer.BirthDate.HasValue
            ? DateTime.Now.Year - customer.BirthDate.Value.Year
            : 0;

        var variables = new Dictionary<string, object>
        {
            ["customer_name"] = customer.FullName,
            ["customer_first_name"] = customer.FirstName,
            ["customer_last_name"] = customer.LastName,
            ["customer_email"] = customer.Email,
            ["customer_phone"] = customer.Phone ?? customer.MobilePhone ?? "",
            ["current_date"] = DateTime.Now.ToString("dd.MM.yyyy"),
            ["current_time"] = DateTime.Now.ToString("HH:mm"),
            ["age"] = age.ToString(),
            ["birthdate"] = customer.BirthDate?.ToString("dd.MM.yyyy") ?? "",
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

    private async Task<bool> ShouldCreateGiftVoucherAsync(Guid companyId, Dictionary<string, object> settings)
    {
        try
        {
            var isEnabled = GetBoolSetting(settings, "enableGiftVoucher", false);

            _logger.LogInformation("Gift voucher enabled check for company {CompanyId}: {IsEnabled}", companyId, isEnabled);

            if (!isEnabled)
            {
                _logger.LogInformation("Gift voucher is disabled for company {CompanyId}", companyId);
                return false;
            }

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
            var giftVoucherSettings = ExtractGiftVoucherSettings(settings);

            var result = await _giftVoucherFactory.CreateGiftVoucherAsync(companyId, customer, giftVoucherSettings);

            if (result.Success)
            {
                _logger.LogInformation("Gift voucher created for birthday customer {CustomerId}: {VoucherCode}",
                    customer.Id, result.VoucherCode);
                return (true, result.VoucherCode, result.Amount);
            }
            else
            {
                _logger.LogWarning("Failed to create gift voucher for birthday customer {CustomerId}: {Message}",
                    customer.Id, result.ErrorMessage);
                return (false, null, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating gift voucher for birthday customer {CustomerId}", customer.Id);
            return (false, null, null);
        }
    }

    private GiftVoucherSettings ExtractGiftVoucherSettings(Dictionary<string, object> settings)
    {
        var giftVoucherSettings = new GiftVoucherSettings
        {
            Description = "Doğum Günü Hediye Çeki"
        };

        if (settings.TryGetValue("giftVoucherAmount", out var amountValue))
        {
            if (amountValue is decimal amount)
                giftVoucherSettings.Amount = (double)amount;
            else if (amountValue is JsonElement amountElement && amountElement.TryGetDecimal(out var amountDecimal))
                giftVoucherSettings.Amount = (double)amountDecimal;
            else if (double.TryParse(amountValue?.ToString(), out var parsedAmount))
                giftVoucherSettings.Amount = parsedAmount;
        }
        else
        {
            giftVoucherSettings.Amount = 50;
        }

        if (settings.TryGetValue("giftVoucherDiscountType", out var discountTypeValue))
        {
            if (discountTypeValue is int discountType)
                giftVoucherSettings.DiscountType = discountType;
            else if (int.TryParse(discountTypeValue?.ToString(), out var parsedDiscountType))
                giftVoucherSettings.DiscountType = parsedDiscountType;
            else if (discountTypeValue is JsonElement discountElement && discountElement.TryGetInt32(out var discountInt))
                giftVoucherSettings.DiscountType = discountInt;
        }
        else
        {
            giftVoucherSettings.DiscountType = 1;
        }

        if (settings.TryGetValue("giftVoucherValidityDays", out var validityValue))
        {
            if (validityValue is int validity)
                giftVoucherSettings.ValidityDays = validity;
            else if (int.TryParse(validityValue?.ToString(), out var parsedValidity))
                giftVoucherSettings.ValidityDays = parsedValidity;
            else if (validityValue is JsonElement validityElement && validityElement.TryGetInt32(out var validityInt))
                giftVoucherSettings.ValidityDays = validityInt;
        }
        else
        {
            giftVoucherSettings.ValidityDays = 365;
        }

        if (settings.TryGetValue("giftVoucherDescription", out var descriptionValue))
        {
            giftVoucherSettings.Description = descriptionValue?.ToString() ?? "Doğum Günü Hediye Çeki";
        }

        giftVoucherSettings.UsageLimit = 1;

        return giftVoucherSettings;
    }

    private async Task LogNotificationAsync(
        Guid companyId,
        int customerId,
        int birthYear,
        string channel,
        string? templateName,
        bool success,
        string? errorMessage,
        decimal cost,
        string? userId,
        bool isAutomatic)
    {
        try
        {
            var notification = new BirthdayNotification
            {
                CompanyId = companyId,
                CustomerId = customerId,
                BirthYear = birthYear,
                Channel = channel,
                TemplateName = templateName,
                Success = success,
                ErrorMessage = errorMessage,
                Cost = cost,
                SentAt = DateTime.Now,
                SentByUserId = userId,
                IsAutomatic = isAutomatic
            };

            _context.BirthdayNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging birthday notification for customer {CustomerId}", customerId);
        }
    }
}
