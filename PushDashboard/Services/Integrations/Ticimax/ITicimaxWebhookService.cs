using PushDashboard.CustomServis;

namespace PushDashboard.Services.Integrations.Ticimax;

public interface ITicimaxWebhookService
{
    /// <summary>
    /// Şirket için gerekli webhook'ları kontrol eder ve eksik olanları ekler
    /// </summary>
    Task<(bool Success, string Message)> EnsureWebhooksAsync(Guid companyId, Dictionary<string, object> settings);

    /// <summary>
    /// Mevcut webhook'ları sorgular
    /// </summary>
    Task<Webhook[]> GetExistingWebhooksAsync(string apiKey, string baseUrl);

    /// <summary>
    /// Yeni webhook kaydeder
    /// </summary>
    Task<(bool Success, int WebhookId, string Message)> SaveWebhookAsync(string apiKey, string baseUrl, WebhookIslem webhookType, string webhookUrl);

    /// <summary>
    /// Webhook'u siler
    /// </summary>
    Task<(bool Success, string Message)> DeleteWebhookAsync(string apiKey, string baseUrl, int webhookId);

    /// <summary>
    /// Sistemin ihtiyaç duyduğu webhook türlerini döner
    /// </summary>
    WebhookIslem[] GetRequiredWebhookTypes();

    /// <summary>
    /// Webhook URL'ini oluşturur
    /// </summary>
    string GenerateWebhookUrl(Guid companyId, WebhookIslem webhookType);
}
