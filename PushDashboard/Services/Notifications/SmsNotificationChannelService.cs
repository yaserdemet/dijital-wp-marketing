using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.SMS;

namespace PushDashboard.Services.Notifications;

public class SmsNotificationChannelService : INotificationChannelService
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsProvider _smsProvider;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly ISmsTemplateService _smsTemplateService;
    private readonly ILogger<SmsNotificationChannelService> _logger;

    public string ChannelType => "sms";

    public SmsNotificationChannelService(
        ApplicationDbContext context,
        ISmsProvider smsProvider,
        IModuleUsageService moduleUsageService,
        ISmsTemplateService smsTemplateService,
        ILogger<SmsNotificationChannelService> logger)
    {
        _context = context;
        _smsProvider = smsProvider;
        _moduleUsageService = moduleUsageService;
        _smsTemplateService = smsTemplateService;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync(Guid companyId)
    {
        try
        {
            // Check if SMS integration is configured
            var smsIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "SMS" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            return smsIntegration != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SMS configuration for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<bool> SendNotificationAsync(Guid companyId, Customer customer, string templateName, Dictionary<string, string> variables)
    {
        try
        {
            // Check if customer has SMS permission
            if (!customer.SmsPermission)
            {
                _logger.LogInformation("Customer {CustomerId} does not have SMS permission. Skipping notification.", customer.Id);
                return false;
            }

            // Get phone number
            var phoneNumber = customer.MobilePhone ?? customer.Phone;
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogWarning("Customer {CustomerId} does not have a valid phone number.", customer.Id);
                return false;
            }

            // Get SMS settings
            var settings = await GetSmsSettingsAsync(companyId);
            if (settings == null)
            {
                _logger.LogWarning("SMS settings not found for company {CompanyId}", companyId);
                return false;
            }

            // Build message from template
            var message = await BuildMessageFromTemplateAsync(companyId, templateName, variables);
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("SMS template '{TemplateName}' not found or empty", templateName);
                return false;
            }

            // Send SMS
            var result = await _smsProvider.SendSmsAsync(phoneNumber, message, settings);

            if (result.Success)
            {
                _logger.LogInformation("SMS notification sent successfully to customer {CustomerId} ({Phone})",
                    customer.Id, phoneNumber);
            }
            else
            {
                _logger.LogWarning("Failed to send SMS notification to customer {CustomerId} ({Phone}): {Error}",
                    customer.Id, phoneNumber, result.Message);
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS notification to customer {CustomerId}", customer.Id);
            return false;
        }
    }

    public async Task<bool> SendNotificationWithCostTrackingAsync(Guid companyId, Customer customer, string templateName,
        Dictionary<string, string> variables, string userId, int? moduleId = null, string? referenceId = null, bool? communicationConsent = null)
    {
        try
        {
            // Check SMS permission
            var hasPermission = communicationConsent ?? customer.SmsPermission;
            if (!hasPermission)
            {
                _logger.LogInformation("Customer {CustomerId} does not have SMS permission. Skipping notification.", customer.Id);
                return false;
            }

            // Get phone number
            var phoneNumber = customer.MobilePhone ?? customer.Phone;
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogWarning("Customer {CustomerId} does not have a valid phone number.", customer.Id);
                return false;
            }

            // Get company
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null)
            {
                _logger.LogWarning("Company {CompanyId} not found for cost tracking", companyId);
                return false;
            }

            // If module specified, check cost and balance
            if (moduleId.HasValue)
            {
                // Check if module is active
                var hasModule = await _moduleUsageService.HasActiveModuleAsync(companyId, moduleId.Value);
                if (!hasModule)
                {
                    _logger.LogWarning("Company {CompanyId} does not have active module {ModuleId} for SMS sending",
                        companyId, moduleId.Value);
                    return false;
                }

                // Get SMS cost
                var cost = await _moduleUsageService.GetModuleUsageCostAsync(companyId, moduleId.Value, "sms");

                // Check balance
                if (company.CreditBalance < cost)
                {
                    _logger.LogWarning("Insufficient balance for company {CompanyId}. Required: {Cost}, Available: {Balance}",
                        companyId, cost, company.CreditBalance);
                    return false;
                }

                // Get SMS settings
                var settings = await GetSmsSettingsAsync(companyId);
                if (settings == null)
                {
                    _logger.LogWarning("SMS settings not found for company {CompanyId}", companyId);
                    return false;
                }

                // Build message
                var message = await BuildMessageFromTemplateAsync(companyId, templateName, variables);
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("SMS template '{TemplateName}' not found or empty", templateName);
                    return false;
                }

                // Send SMS
                var sendResult = await _smsProvider.SendSmsAsync(phoneNumber, message, settings);

                if (sendResult.Success)
                {
                    // Deduct cost on successful send
                    var deductResult = await _moduleUsageService.DeductUsageCostAsync(
                        companyId: companyId,
                        moduleId: moduleId.Value,
                        usageType: "Sms Bildirimi",
                        cost: cost,
                        description: $"{templateName} SMS gönderildi: {customer.FullName} ({phoneNumber})",
                        userId: userId,
                        referenceId: referenceId ?? customer.Id.ToString(),
                        channel: "sms",
                        metadata: new Dictionary<string, object>
                        {
                            ["phoneNumber"] = phoneNumber,
                            ["templateName"] = templateName,
                            ["messageId"] = sendResult.MessageId ?? "",
                            ["sentAt"] = DateTime.Now
                        }
                    );

                    if (!deductResult.Success)
                    {
                        _logger.LogError("Failed to deduct cost for SMS sending. {Message}", deductResult.Message);
                    }

                    _logger.LogInformation("SMS notification with cost tracking sent successfully to customer {CustomerId} ({Phone})",
                        customer.Id, phoneNumber);
                }
                else
                {
                    _logger.LogWarning("Failed to send SMS notification with cost tracking to customer {CustomerId} ({Phone}): {Error}",
                        customer.Id, phoneNumber, sendResult.Message);
                }

                return sendResult.Success;
            }
            else
            {
                // No module specified, send without cost tracking
                return await SendNotificationAsync(companyId, customer, templateName, variables);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS notification with cost tracking to customer {CustomerId}", customer.Id);
            return false;
        }
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(Guid companyId)
    {
        try
        {
            // Get SMS integration
            var smsIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "SMS" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            if (smsIntegration == null)
            {
                return ConnectionTestResult.CreateFailure(
                    "SMS entegrasyonu bulunamadı veya yapılandırılmamış.",
                    "SMS_NOT_CONFIGURED",
                    "SMS"
                );
            }

            // Get settings
            var settingsDict = smsIntegration.Settings;
            if (settingsDict == null || !settingsDict.Any())
            {
                return ConnectionTestResult.CreateFailure(
                    "SMS ayarları bulunamadı.",
                    "SMS_SETTINGS_MISSING",
                    "SMS"
                );
            }

            // Test with current settings
            return await TestConnectionWithSettingsAsync(companyId, settingsDict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMS connection for company {CompanyId}", companyId);
            return ConnectionTestResult.CreateError(ex, "SMS");
        }
    }

    public async Task<ConnectionTestResult> TestConnectionWithSettingsAsync(Guid companyId, Dictionary<string, object> settingsDict)
    {
        try
        {
            // Parse settings
            var settings = ParseSettings(settingsDict);
            if (settings == null)
            {
                return ConnectionTestResult.CreateFailure(
                    "SMS ayarları geçersiz.",
                    "INVALID_SETTINGS",
                    "SMS"
                );
            }

            // Test connection using provider
            var testResult = await _smsProvider.TestConnectionAsync(settings);

            if (testResult.Success)
            {
                var message = testResult.AccountBalance.HasValue
                    ? $"SMS bağlantısı başarılı! Bakiye: {testResult.AccountBalance.Value:N2} TL"
                    : "SMS bağlantısı başarılı!";

                return ConnectionTestResult.CreateSuccess(
                    message,
                    "SMS",
                    testResult.ResponseTime
                );
            }
            else
            {
                return ConnectionTestResult.CreateFailure(
                    testResult.Message,
                    testResult.ErrorCode ?? "TEST_FAILED",
                    "SMS"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMS connection with settings for company {CompanyId}", companyId);
            return ConnectionTestResult.CreateError(ex, "SMS");
        }
    }

    #region Helper Methods

    private async Task<SmsSettings?> GetSmsSettingsAsync(Guid companyId)
    {
        try
        {
            var smsIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "SMS" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            if (smsIntegration?.Settings == null)
                return null;

            return ParseSettings(smsIntegration.Settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMS settings for company {CompanyId}", companyId);
            return null;
        }
    }

    private SmsSettings? ParseSettings(Dictionary<string, object> settingsDict)
    {
        try
        {
            var username = settingsDict.GetValueOrDefault("username")?.ToString();
            var password = settingsDict.GetValueOrDefault("password")?.ToString();
            var senderName = settingsDict.GetValueOrDefault("senderName")?.ToString();
            var providerStr = settingsDict.GetValueOrDefault("provider")?.ToString();
            var autoNotificationsStr = settingsDict.GetValueOrDefault("autoNotifications")?.ToString();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            // Parse provider
            var provider = SmsProvider.NetGsm; // Default
            if (!string.IsNullOrWhiteSpace(providerStr))
            {
                Enum.TryParse<SmsProvider>(providerStr, true, out provider);
            }

            // Parse auto notifications
            var autoNotifications = true; // Default
            if (!string.IsNullOrWhiteSpace(autoNotificationsStr))
            {
                bool.TryParse(autoNotificationsStr, out autoNotifications);
            }

            return new SmsSettings
            {
                Provider = provider,
                Username = username,
                Password = password,
                SenderName = senderName ?? "",
                AutoNotifications = autoNotifications
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing SMS settings");
            return null;
        }
    }

    private async Task<string?> BuildMessageFromTemplateAsync(Guid companyId, string templateName, Dictionary<string, string> variables)
    {
        try
        {
            // Try to get template from service first
            var templates = await _smsTemplateService.GetUserTemplatesAsync(companyId);
            var template = templates.FirstOrDefault(t => t.Name == templateName);
            
            string? message = null;

            if (template != null)
            {
                message = template.CustomContent;
            }
            
            if (string.IsNullOrEmpty(message))
            {
                // Fallback to hardcoded default if not found in DB
                message = GetDefaultTemplate(templateName);
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            // Replace variables
            foreach (var variable in variables)
            {
                message = message.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building message from template '{TemplateName}'", templateName);
            return null;
        }
    }

    private string? GetDefaultTemplate(string templateName)
    {
        // Default SMS templates
        // These should match the template names used in modules
        return templateName switch
        {
            "basket_reminder" => "Merhaba {{customerName}}, sepetinizde ürünler bekliyor! {{basketUrl}}",
            "pending_payment" => "{{customerName}}, bekleyen ödemeniz bulunmaktadır. Sipariş No: {{orderNumber}}",
            "order_status" => "Sipariş durumu: {{orderStatus}}. Sipariş No: {{orderNumber}}",
            "first_order" => "Hoş geldiniz {{customerName}}! İlk siparişiniz için teşekkür ederiz.",
            "birthday" => "Mutlu yıllar {{customerName}}! Size özel indirim kodunuz: {{couponCode}}",
            _ => null
        };
    }

    #endregion
}
