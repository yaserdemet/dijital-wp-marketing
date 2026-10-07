using Microsoft.AspNetCore.Mvc;
using PushDashboard.Services;

namespace PushDashboard.Controllers.Api;

[ApiController]
[Route("api/contact")]
public class ContactApiController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactApiController> _logger;
    private const string TargetEmail = "info@pushonica.com";

    public ContactApiController(
        IEmailService emailService,
        ILogger<ContactApiController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// İletişim formu üzerinden mail gönderir
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult> SendContactEmail([FromBody] ContactEmailRequest request)
    {
        
        if (request == null)
        {
            return BadRequest(new { success = false, message = "Geçersiz istek" });
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return BadRequest(new { success = false, message = "Konu alanı zorunludur" });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { success = false, message = "Mesaj alanı zorunludur" });
        }

        _logger.LogInformation("Contact email request received. Subject: {Subject}, From: {FromEmail}", 
            request.Subject, request.FromEmail ?? "Belirtilmemiş");

        var htmlBody = BuildEmailBody(request);

        var result = await _emailService.SendEmailAsync(
            toEmail: TargetEmail,
            subject: $"[İletişim Formu] {request.Subject}",
            htmlBody: htmlBody,
            plainTextBody: request.Message
        );

        if (result)
        {
            _logger.LogInformation("Contact email sent successfully to {TargetEmail}", TargetEmail);
            return Ok(new { success = true, message = "Mail başarıyla gönderildi" });
        }
        else
        {
            _logger.LogWarning("Failed to send contact email to {TargetEmail}", TargetEmail);
            return StatusCode(500, new { success = false, message = "Mail gönderilemedi. Lütfen daha sonra tekrar deneyin." });
        }
    }

    private static string BuildEmailBody(ContactEmailRequest request)
    {
        var fromInfo = !string.IsNullOrWhiteSpace(request.FromEmail) 
            ? $"<p><strong>Gönderen:</strong> {request.FromEmail}</p>" 
            : "";
        
        var nameInfo = !string.IsNullOrWhiteSpace(request.Name) 
            ? $"<p><strong>İsim:</strong> {request.Name}</p>" 
            : "";
        
        var phoneInfo = !string.IsNullOrWhiteSpace(request.Phone) 
            ? $"<p><strong>Telefon:</strong> {request.Phone}</p>" 
            : "";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9fafb; padding: 20px; border: 1px solid #e5e7eb; border-radius: 0 0 8px 8px; }}
        .info {{ background-color: white; padding: 15px; border-radius: 8px; margin-bottom: 15px; }}
        .message {{ background-color: white; padding: 15px; border-radius: 8px; white-space: pre-wrap; }}
        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #6b7280; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>📧 Yeni İletişim Formu Mesajı</h2>
        </div>
        <div class='content'>
            <div class='info'>
                <p><strong>Konu:</strong> {request.Subject}</p>
                {nameInfo}
                {fromInfo}
                {phoneInfo}
            </div>
            <h3>Mesaj:</h3>
            <div class='message'>{request.Message}</div>
        </div>
        <div class='footer'>
            <p>Bu mail Dijitaldesin iletişim formu üzerinden gönderilmiştir.</p>
            <p>Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}</p>
        </div>
    </div>
</body>
</html>";
    }
}

public class ContactEmailRequest
{
    /// <summary>
    /// Mail konusu (zorunlu)
    /// </summary>
    public string Subject { get; set; } = string.Empty;
    
    /// <summary>
    /// Mail içeriği (zorunlu)
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Gönderen kişinin email adresi (opsiyonel)
    /// </summary>
    public string? FromEmail { get; set; }
    
    /// <summary>
    /// Gönderen kişinin adı (opsiyonel)
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gönderen kişinin telefon numarası (opsiyonel)
    /// </summary>
    public string? Phone { get; set; }
}

