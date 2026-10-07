using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Models;
using PushDashboard.Data;

namespace PushDashboard.Services;

public interface IEmailService
{
    Task<bool> SendInvitationEmailAsync(string toEmail, string inviterName, string companyName, string invitationLink);
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null);
    Task<bool> SendEmailWithCompanySmtpAsync(string toEmail, string subject, string htmlBody, Guid companyId, string? plainTextBody = null);
    Task<bool> SendEmailWithCostTrackingAsync(string toEmail, string subject, string htmlBody, Guid companyId, int? moduleId = null, string? description = null, string? referenceId = null, string? plainTextBody = null);
    Task<bool> IsConfiguredAsync();
    Task<bool> SendTestEmailAsync(string smtpServer, int port, string username, string password, bool sslEnabled, string fromEmail, string fromName, string toEmail);
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly ApplicationDbContext _context;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<EmailSettings> emailSettings,
        IModuleUsageService moduleUsageService,
        ApplicationDbContext context)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
        _moduleUsageService = moduleUsageService;
        _context = context;
    }

    public async Task<bool> SendInvitationEmailAsync(string toEmail, string inviterName, string companyName, string invitationLink)
    {
        var subject = $"{companyName} - Kullanıcı Davetiyesi";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Kullanıcı Davetiyesi</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background-color: #4F46E5; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
        .warning {{ background-color: #FEF3C7; border: 1px solid #F59E0B; padding: 15px; border-radius: 6px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Kullanıcı Davetiyesi</h1>
        </div>
        <div class='content'>
            <h2>Merhaba!</h2>
            <p><strong>{inviterName}</strong> sizi <strong>{companyName}</strong> firmasının Dijitaldesin Admin paneline katılmaya davet ediyor.</p>

            <p>Davetiyeyi kabul etmek ve hesabınızı oluşturmak için aşağıdaki butona tıklayın:</p>

            <div style='text-align: center;'>
                <a href='{invitationLink}' class='button'>Davetiyeyi Kabul Et</a>
            </div>

            <div class='warning'>
                <strong>⚠️ Önemli:</strong> Bu davetiye 12 saat geçerlidir. Süre dolmadan önce hesabınızı oluşturmayı unutmayın.
            </div>

            <p>Eğer bu davetiyeyi beklemiyorsanız, bu e-postayı görmezden gelebilirsiniz.</p>

            <p>Saygılarımızla,<br>Dijitaldesin Admin Ekibi</p>
        </div>
        <div class='footer'>
            <p>Bu e-posta otomatik olarak gönderilmiştir. Lütfen yanıtlamayın.</p>
        </div>
    </div>
</body>
</html>";

        var plainTextBody = $@"
Kullanıcı Davetiyesi

Merhaba!

{inviterName} sizi {companyName} firmasının Dijitaldesin Admin paneline katılmaya davet ediyor.

Davetiyeyi kabul etmek için aşağıdaki bağlantıya tıklayın:
{invitationLink}

⚠️ Önemli: Bu davetiye 12 saat geçerlidir.

Eğer bu davetiyeyi beklemiyorsanız, bu e-postayı görmezden gelebilirsiniz.

Saygılarımızla,
Dijitaldesin Admin Ekibi
";

        return await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
    }

    public async Task<bool> SendEmailWithCompanySmtpAsync(string toEmail, string subject, string htmlBody, Guid companyId, string? plainTextBody = null)
    {
        try
        {
            // Şirketin SMTP entegrasyonunu al
            var smtpIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "Email SMTP" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            if (smtpIntegration == null || smtpIntegration.Settings == null || !smtpIntegration.Settings.Any())
            {
                _logger.LogWarning("Company {CompanyId} does not have configured SMTP integration. Falling back to global settings.", companyId);
                return await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
            }

            var settings = smtpIntegration.Settings;
            var smtpServer = settings.GetValueOrDefault("smtpServer")?.ToString();
            var portStr = settings.GetValueOrDefault("port")?.ToString() ?? settings.GetValueOrDefault("smtpPort")?.ToString();
            var username = settings.GetValueOrDefault("username")?.ToString();
            var password = settings.GetValueOrDefault("password")?.ToString();
            var fromEmail = settings.GetValueOrDefault("fromEmail")?.ToString();
            var fromName = settings.GetValueOrDefault("fromName")?.ToString();
            var sslEnabledStr = settings.GetValueOrDefault("sslEnabled")?.ToString() ?? settings.GetValueOrDefault("enableSsl")?.ToString();

            // Gerekli alanları kontrol et
            if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogWarning("Company {CompanyId} SMTP settings are incomplete. Falling back to global settings.", companyId);
                return await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
            }

            if (!int.TryParse(portStr, out var port))
                port = 587;

            if (!bool.TryParse(sslEnabledStr, out var sslEnabled))
                sslEnabled = true;

            _logger.LogInformation("Sending email to {ToEmail} using company {CompanyId} SMTP settings via {SmtpServer}:{Port}",
                toEmail, companyId, smtpServer, port);

            using var smtpClient = new SmtpClient(smtpServer, port);
            smtpClient.EnableSsl = sslEnabled;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(username, password);

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail, fromName ?? fromEmail);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = subject;
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = htmlBody;

            if (!string.IsNullOrEmpty(plainTextBody))
            {
                var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain");
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
                mailMessage.AlternateViews.Add(plainTextView);
                mailMessage.AlternateViews.Add(htmlView);
                mailMessage.Body = string.Empty;
            }

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("Email successfully sent to {ToEmail} using company {CompanyId} SMTP", toEmail, companyId);
            return true;
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error while sending email to {ToEmail} for company {CompanyId}: {ErrorMessage}",
                toEmail, companyId, smtpEx.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} for company {CompanyId}", toEmail, companyId);
            return false;
        }
    }

    public async Task<bool> SendEmailWithCostTrackingAsync(string toEmail, string subject, string htmlBody, Guid companyId, int? moduleId = null, string? description = null, string? referenceId = null, string? plainTextBody = null)
    {
        try
        {
            // Kullanıcının şirket bilgisini al
            var company = await _context.Companies.FirstOrDefaultAsync(u => u.Id == companyId);
            if (company == null)
            {
                _logger.LogWarning("Company: {companyId} or company not found for cost tracking", companyId);
                return await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody); // Fallback to normal send
            }

            // Eğer modül belirtilmişse maliyet kontrolü yap
            if (moduleId.HasValue)
            {
                // Modülün aktif olup olmadığını kontrol et
                var hasModule = await _moduleUsageService.HasActiveModuleAsync(companyId, moduleId.Value);
                if (!hasModule)
                {
                    _logger.LogWarning("Company {companyId} does not have active module {ModuleId} for email sending", companyId, moduleId.Value);
                    return false;
                }

                // Maliyet hesapla
                var cost = await _moduleUsageService.GetModuleUsageCostAsync(companyId, moduleId.Value, "email");

                // Bakiye kontrolü
                if (company.CreditBalance < cost)
                {
                    _logger.LogWarning("Insufficient balance for company {CompanyId}. Required: {Cost}, Available: {Balance}",
                        companyId, cost, company.CreditBalance);
                    return false;
                }

                // Email gönder - şirketin SMTP ayarlarını kullan
                var emailSent = await SendEmailWithCompanySmtpAsync(toEmail, subject, htmlBody, companyId, plainTextBody);

                if (emailSent)
                {
                    // Başarılı gönderim durumunda maliyeti düş
                    var deductResult = await _moduleUsageService.DeductUsageCostAsync(
                        companyId: companyId,
                        moduleId: moduleId.Value,
                        usageType: "Email Gönderimi",
                        cost: cost,
                        description: description ?? $"Email {toEmail} adresine gönderildi.",
                        userId: "30eb145f-1cbf-4250-8efa-829f617fac72", // TODO : Current user ID'yi al
                        referenceId: referenceId,
                        channel: "email",
                        metadata: new Dictionary<string, object>
                        {
                            ["toEmail"] = toEmail,
                            ["subject"] = subject,
                            ["sentAt"] = DateTime.Now
                        }
                    );

                    if (!deductResult.Success)
                    {
                        _logger.LogError("Failed to deduct cost for email sending. {Message}", deductResult.Message);
                    }
                }

                return emailSent;
            }
            else
            {
                // Modül belirtilmemişse şirket SMTP'si ile gönderim
                return await SendEmailWithCompanySmtpAsync(toEmail, subject, htmlBody, companyId, plainTextBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendEmailWithCostTrackingAsync for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null)
    {
        try
        {
            // Check if email settings are configured
            if (!await IsConfiguredAsync())
            {
                _logger.LogWarning("Email settings are not configured. Cannot send email to {ToEmail}", toEmail);
                return false;
            }

            _logger.LogInformation("Sending email to {ToEmail} with subject: {Subject}", toEmail, subject);

            
            using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
            smtpClient.EnableSsl = _emailSettings.EnableSsl;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = subject;
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = htmlBody;

            // Add plain text alternative if provided
            if (!string.IsNullOrEmpty(plainTextBody))
            {
                var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain");
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");

                mailMessage.AlternateViews.Add(plainTextView);
                mailMessage.AlternateViews.Add(htmlView);
                mailMessage.Body = string.Empty; // Clear body when using AlternateViews
            }

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("Email successfully sent to {ToEmail}", toEmail);
            return true;
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error while sending email to {ToEmail}: {ErrorMessage}", toEmail, smtpEx.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            return false;
        }
    }

    public async Task<bool> IsConfiguredAsync()
    {
        await Task.CompletedTask;

        return !string.IsNullOrEmpty(_emailSettings.SmtpServer) &&
               _emailSettings.SmtpPort > 0 &&
               !string.IsNullOrEmpty(_emailSettings.SenderEmail) &&
               !string.IsNullOrEmpty(_emailSettings.Username) &&
               !string.IsNullOrEmpty(_emailSettings.Password);
    }

    public async Task<bool> SendTestEmailAsync(string smtpServer, int port, string username, string password, bool sslEnabled, string fromEmail, string fromName, string toEmail)
    {
        try
        {
            _logger.LogInformation("Testing SMTP connection to {SmtpServer}:{Port} with SSL: {SslEnabled}", smtpServer, port, sslEnabled);

            using var smtpClient = new SmtpClient(smtpServer, port);
            smtpClient.EnableSsl = sslEnabled;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(username, password);
            smtpClient.Timeout = 30000; // 30 seconds timeout

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(fromEmail, fromName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = "SMTP Bağlantı Testi - Dijitaldesin";
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>SMTP Test</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #10B981; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
        .success {{ background-color: #D1FAE5; border: 1px solid #10B981; padding: 15px; border-radius: 6px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ SMTP Bağlantı Testi Başarılı</h1>
        </div>
        <div class='content'>
            <div class='success'>
                <strong>Tebrikler!</strong> SMTP ayarlarınız doğru yapılandırılmış ve email gönderimi çalışıyor.
            </div>

            <h3>Test Detayları:</h3>
            <ul>
                <li><strong>SMTP Sunucu:</strong> {smtpServer}</li>
                <li><strong>Port:</strong> {port}</li>
                <li><strong>SSL/TLS:</strong> {(sslEnabled ? "Etkin" : "Devre Dışı")}</li>
                <li><strong>Gönderen:</strong> {fromEmail}</li>
                <li><strong>Test Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm:ss}</li>
            </ul>

            <p>Bu test emaili, SMTP entegrasyonunuzun düzgün çalıştığını doğrulamak için gönderilmiştir.</p>

            <p>Saygılarımızla,<br>Dijitaldesin Admin Ekibi</p>
        </div>
        <div class='footer'>
            <p>Bu e-posta otomatik olarak gönderilmiştir.</p>
        </div>
    </div>
</body>
</html>";

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("Test email successfully sent to {ToEmail} via {SmtpServer}:{Port}", toEmail, smtpServer, port);
            return true;
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error during test email to {ToEmail} via {SmtpServer}:{Port}: {ErrorMessage}",
                toEmail, smtpServer, port, smtpEx.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test email to {ToEmail} via {SmtpServer}:{Port}",
                toEmail, smtpServer, port);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var subject = "Dijitaldesin - Şifre Sıfırlama";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Şifre Sıfırlama</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background-color: #4F46E5; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
        .warning {{ background-color: #FEF3C7; border: 1px solid #F59E0B; padding: 15px; border-radius: 6px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Şifre Sıfırlama</h1>
        </div>
        <div class='content'>
            <h2>Merhaba!</h2>
            <p>Dijitaldesin hesabınız için bir şifre sıfırlama talebinde bulunuldu.</p>

            <p>Şifrenizi sıfırlamak için aşağıdaki butona tıklayın:</p>

            <div style='text-align: center;'>
                <a href='{resetLink}' class='button'>Şifremi Sıfırla</a>
            </div>

            <div class='warning'>
                <strong>⚠️ Önemli:</strong> Bu bağlantı güvenlik nedeniyle <strong>1 saat</strong> geçerlidir. Süre dolduktan sonra yeni bir şifre sıfırlama talebinde bulunmanız gerekecektir.
            </div>

            <p>Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz. Şifreniz değiştirilmeyecektir.</p>

            <p>Saygılarımızla,<br>Dijitaldesin Ekibi</p>
        </div>
        <div class='footer'>
            <p>Bu e-posta otomatik olarak gönderilmiştir. Lütfen yanıtlamayın.</p>
        </div>
    </div>
</body>
</html>";

        var plainTextBody = $@"
Şifre Sıfırlama

Merhaba!

Dijitaldesin hesabınız için bir şifre sıfırlama talebinde bulunuldu.

Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:
{resetLink}

⚠️ Önemli: Bu bağlantı 1 saat geçerlidir.

Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.

Saygılarımızla,
Dijitaldesin Ekibi";

        return await SendEmailAsync(toEmail, subject, htmlBody, plainTextBody);
    }
}
