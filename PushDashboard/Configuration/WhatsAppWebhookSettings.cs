namespace PushDashboard.Configuration;

/// <summary>
/// WhatsApp webhook ayarları
/// </summary>
public class WhatsAppWebhookSettings
{
    public const string SectionName = "WhatsAppWebhook";

    /// <summary>
    /// Webhook doğrulama token'ı - Gupshup'tan gelen istekleri doğrulamak için
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// Application base URL (webhook URL'i oluşturmak için)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}
