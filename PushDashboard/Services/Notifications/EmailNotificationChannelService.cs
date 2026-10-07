using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Services.Notifications;

public class EmailNotificationChannelService : INotificationChannelService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly ILogger<EmailNotificationChannelService> _logger;

    public string ChannelType => "email";

    public EmailNotificationChannelService(
        ApplicationDbContext context,
        IEmailService emailService,
        IModuleUsageService moduleUsageService,
        ILogger<EmailNotificationChannelService> logger)
    {
        _context = context;
        _emailService = emailService;
        _moduleUsageService = moduleUsageService;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync(Guid companyId)
    {
        try
        {
            // SMTP entegrasyonunun yapılandırılıp yapılandırılmadığını kontrol et
            var smtpIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "Email SMTP" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            return smtpIntegration != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SMTP configuration for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<bool> SendNotificationAsync(Guid companyId, Customer customer, string templateName, Dictionary<string, string> variables)
    {
        try
        {
            // Müşterinin email izni var mı kontrol et
            if (!customer.EmailPermission)
            {
                _logger.LogInformation("Customer {CustomerId} does not have email permission. Skipping notification.", customer.Id);
                return false;
            }

            string subject;
            string content;

            var customTemplate = await _context.CompanyEmailTemplates
                .Where(cet => cet.CompanyId == companyId &&
                              cet.Name == templateName &&
                              cet.IsEnabled)
                .FirstOrDefaultAsync();

            if (customTemplate != null)
            {
                subject = customTemplate.CustomSubject ?? "Bildirim";
                content = customTemplate.CustomContent;
                _logger.LogInformation("Using custom template '{TemplateName}' for company {CompanyId}", templateName, companyId);
            }
            else
            {
                // Custom template bulunamadı, base template'i kullan
                var result = await GetBaseTemplateContent(companyId, templateName);
                if (!result.found)
                {
                    _logger.LogWarning("Email template '{TemplateName}' not found", templateName);
                    return false;
                }
                subject = result.subject;
                content = result.content;
            }

            // Template değişkenlerini değiştir
            foreach (var variable in variables)
            {
                subject = subject.Replace($"{{{{{variable.Key}}}}}", variable.Value);
                content = content.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }

            // Email gönder
            var emailSent = await _emailService.SendEmailAsync(customer.Email, subject, content);

            if (emailSent)
            {
                _logger.LogInformation("Birthday notification sent successfully to customer {CustomerId} ({Email})",
                    customer.Id, customer.Email);
            }
            else
            {
                _logger.LogWarning("Failed to send birthday notification to customer {CustomerId} ({Email})",
                    customer.Id, customer.Email);
            }

            return emailSent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification to customer {CustomerId}", customer.Id);
            return false;
        }
    }

    public async Task<bool> SendNotificationWithCostTrackingAsync(Guid companyId, Customer customer, string templateName, Dictionary<string, string> variables, string userId, int? moduleId = null, string? referenceId = null, bool? communicationConsent = null)
    {
        try
        {
            // Müşterinin email izni var mı kontrol et
            var hasPermission = communicationConsent ?? customer.EmailPermission;
            if (!hasPermission)
            {
                _logger.LogInformation("Customer {CustomerId} does not have email permission. Skipping notification.", customer.Id);
                return false;
            }

            string subject;
            string content;

            // Önce custom template'leri kontrol et (templateName ile eşleşen)
            var customTemplate = await _context.CompanyEmailTemplates
                .Where(cet => cet.CompanyId == companyId &&
                             cet.Name == templateName &&
                             cet.IsEnabled)
                .FirstOrDefaultAsync();

            if (customTemplate != null)
            {
                subject = customTemplate.CustomSubject ?? "Bildirim";
                content = customTemplate.CustomContent;
                _logger.LogInformation("Using custom template '{TemplateName}' for company {CompanyId}", templateName, companyId);
            }
            else
            {
                // Custom template bulunamadı, base template'i kullan
                var result = await GetBaseTemplateContent(companyId, templateName);
                if (!result.found)
                {
                    _logger.LogWarning("Email template '{TemplateName}' not found", templateName);
                    return false;
                }
                subject = result.subject;
                content = result.content;
            }

            // Template değişkenlerini değiştir
            foreach (var variable in variables)
            {
                subject = subject.Replace($"{{{{{variable.Key}}}}}", variable.Value);
                content = content.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }

            // Maliyet takibi ile email gönder
            var emailSent = await _emailService.SendEmailWithCostTrackingAsync(
                toEmail: customer.Email,
                subject: subject,
                htmlBody: content,
                companyId: companyId,
                moduleId: moduleId,
                description: $"{templateName} bildirimi gönderildi: {customer.FullName} ({customer.Email})",
                referenceId: referenceId ?? customer.Id.ToString()
            );

            if (emailSent)
            {
                _logger.LogInformation("Email notification with cost tracking sent successfully to customer {CustomerId} ({Email})",
                    customer.Id, customer.Email);
            }
            else
            {
                _logger.LogWarning("Failed to send email notification with cost tracking to customer {CustomerId} ({Email})",
                    customer.Id, customer.Email);
            }

            return emailSent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification with cost tracking to customer {CustomerId}", customer.Id);
            return false;
        }
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(Guid companyId)
    {
        try
        {
            // SMTP entegrasyonunu bul
            var smtpIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "Email SMTP" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            if (smtpIntegration == null)
            {
                return ConnectionTestResult.CreateFailure(
                    "SMTP entegrasyonu bulunamadı veya yapılandırılmamış.",
                    "SMTP_NOT_CONFIGURED",
                    "Email SMTP"
                );
            }

            // SMTP ayarlarını al
            var settings = smtpIntegration.Settings;
            if (settings == null || !settings.Any())
            {
                return ConnectionTestResult.CreateFailure(
                    "SMTP ayarları bulunamadı.",
                    "SMTP_SETTINGS_MISSING",
                    "Email SMTP"
                );
            }

            // Mevcut ayarlarla test et
            return await TestConnectionWithSettingsAsync(companyId, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMTP connection for company {CompanyId}", companyId);
            return ConnectionTestResult.CreateError(ex, "Email SMTP");
        }
    }

    public async Task<ConnectionTestResult> TestConnectionWithSettingsAsync(Guid companyId, Dictionary<string, object> settings)
    {
        try
        {
            // Ayarları validate et ve parse et
            var validationResult = ValidateSmtpSettings(settings);
            if (!validationResult.IsValid)
            {
                return validationResult.ErrorResult!;
            }

            var smtpConfig = validationResult.SmtpConfig!;

            // Test email gönder
            var startTime = DateTime.Now;
            var testEmailSent = await _emailService.SendTestEmailAsync(
                smtpServer: smtpConfig.SmtpServer,
                port: smtpConfig.Port,
                username: smtpConfig.Username,
                password: smtpConfig.Password,
                sslEnabled: smtpConfig.SslEnabled,
                fromEmail: smtpConfig.FromEmail,
                fromName: smtpConfig.FromName,
                toEmail: smtpConfig.FromEmail // Kendine test email gönder
            );
            var endTime = DateTime.Now;
            var responseTime = endTime - startTime;

            if (testEmailSent)
            {
                return ConnectionTestResult.CreateSuccess(
                    "SMTP bağlantısı başarılı! Test email gönderildi.",
                    "Email SMTP",
                    responseTime
                );
            }
            else
            {
                return ConnectionTestResult.CreateFailure(
                    "SMTP bağlantısı başarısız. Test email gönderilemedi.",
                    "SMTP_TEST_FAILED",
                    "Email SMTP"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMTP connection with settings for company {CompanyId}", companyId);
            return ConnectionTestResult.CreateError(ex, "Email SMTP");
        }
    }

    #region Helper Methods

    private (string name, string category, string description, string cleanContent) ParseCustomTemplateMetadata(string content)
    {
        var name = "Özel Şablon";
        var category = "Özel";
        var description = "";
        var cleanContent = content;

        // Look for metadata comment at the beginning
        if (content.StartsWith("<!-- CUSTOM_TEMPLATE:"))
        {
            var endIndex = content.IndexOf("-->");
            if (endIndex > 0)
            {
                var metadataLine = content.Substring(0, endIndex + 3);
                cleanContent = content.Substring(endIndex + 3).TrimStart('\n', '\r');

                // Parse metadata: <!-- CUSTOM_TEMPLATE: name | category | description -->
                var metadataContent = metadataLine.Replace("<!-- CUSTOM_TEMPLATE:", "").Replace("-->", "").Trim();
                var parts = metadataContent.Split('|');

                if (parts.Length >= 1) name = parts[0].Trim();
                if (parts.Length >= 2) category = parts[1].Trim();
                if (parts.Length >= 3) description = parts[2].Trim();
            }
        }

        return (name, category, description, cleanContent);
    }

    private async Task<(bool found, string subject, string content)> GetBaseTemplateContent(Guid companyId, string templateName)
    {
        try
        {
            // Email template'ini bul
            var emailTemplate = await _context.EmailTemplates
                .FirstOrDefaultAsync(et => et.Name == templateName && et.IsActive);

            if (emailTemplate == null)
            {
                return (false, "", "");
            }

            // Şirketin özelleştirdiği template'i kontrol et
            var companyTemplate = await _context.CompanyEmailTemplates
                .FirstOrDefaultAsync(cet => cet.CompanyId == companyId &&
                                          cet.EmailTemplateId == emailTemplate.Id &&
                                          cet.IsEnabled);

            string subject = companyTemplate?.CustomSubject ?? emailTemplate.DefaultSubject ?? "Bildirim";
            string content = companyTemplate?.CustomContent ?? emailTemplate.DefaultContent;

            return (true, subject, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting base template content for template '{TemplateName}'", templateName);
            return (false, "", "");
        }
    }

    private SmtpValidationResult ValidateSmtpSettings(Dictionary<string, object> settings)
    {
        // Settings'lerden SMTP bilgilerini al
        var smtpServer = settings.GetValueOrDefault("smtpServer")?.ToString();
        var portStr = settings.GetValueOrDefault("port")?.ToString() ??
                     settings.GetValueOrDefault("smtpPort")?.ToString(); // Controller'dan gelen farklı key
        var username = settings.GetValueOrDefault("username")?.ToString();
        var password = settings.GetValueOrDefault("password")?.ToString();
        var fromEmail = settings.GetValueOrDefault("fromEmail")?.ToString();
        var fromName = settings.GetValueOrDefault("fromName")?.ToString();
        var sslEnabledStr = settings.GetValueOrDefault("sslEnabled")?.ToString() ??
                           settings.GetValueOrDefault("enableSsl")?.ToString(); // Controller'dan gelen farklı key

        // Validation
        if (string.IsNullOrWhiteSpace(smtpServer))
        {
            return SmtpValidationResult.Invalid(ConnectionTestResult.CreateFailure(
                "SMTP sunucu adresi gereklidir.",
                "SMTP_SERVER_MISSING",
                "Email SMTP"
            ));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            return SmtpValidationResult.Invalid(ConnectionTestResult.CreateFailure(
                "Kullanıcı adı gereklidir.",
                "USERNAME_MISSING",
                "Email SMTP"
            ));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return SmtpValidationResult.Invalid(ConnectionTestResult.CreateFailure(
                "Şifre gereklidir.",
                "PASSWORD_MISSING",
                "Email SMTP"
            ));
        }

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            return SmtpValidationResult.Invalid(ConnectionTestResult.CreateFailure(
                "Gönderen email adresi gereklidir.",
                "FROM_EMAIL_MISSING",
                "Email SMTP"
            ));
        }

        // Parse port
        if (!int.TryParse(portStr, out var port))
        {
            port = 587; // Default SMTP port
        }

        // Parse SSL
        if (!bool.TryParse(sslEnabledStr, out var sslEnabled))
        {
            sslEnabled = true; // Default to SSL enabled
        }

        var smtpConfig = new SmtpConfig
        {
            SmtpServer = smtpServer,
            Port = port,
            Username = username,
            Password = password,
            FromEmail = fromEmail,
            FromName = fromName ?? "Test Sender",
            SslEnabled = sslEnabled
        };

        return SmtpValidationResult.Valid(smtpConfig);
    }

    #endregion
}

#region Helper Classes

public class SmtpConfig
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool SslEnabled { get; set; }
}

public class SmtpValidationResult
{
    public bool IsValid { get; set; }
    public SmtpConfig? SmtpConfig { get; set; }
    public ConnectionTestResult? ErrorResult { get; set; }

    public static SmtpValidationResult Valid(SmtpConfig config)
    {
        return new SmtpValidationResult
        {
            IsValid = true,
            SmtpConfig = config
        };
    }

    public static SmtpValidationResult Invalid(ConnectionTestResult errorResult)
    {
        return new SmtpValidationResult
        {
            IsValid = false,
            ErrorResult = errorResult
        };
    }
}

#endregion
