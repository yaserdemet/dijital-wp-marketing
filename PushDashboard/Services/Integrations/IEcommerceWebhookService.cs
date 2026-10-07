namespace PushDashboard.Services.Integrations;

/// <summary>
/// E-ticaret platformları için genel webhook yönetim interface'i
/// </summary>
public interface IEcommerceWebhookService
{
    /// <summary>
    /// Bu servisin desteklediği e-ticaret platformu adı
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// Şirket için gerekli webhook'ları kontrol eder ve eksik olanları ekler
    /// </summary>
    Task<(bool Success, string Message)> EnsureWebhooksAsync(Guid companyId, Dictionary<string, object> settings);

    /// <summary>
    /// Mevcut webhook'ları sorgular
    /// </summary>
    Task<object[]> GetExistingWebhooksAsync(Dictionary<string, object> settings);

    /// <summary>
    /// Yeni webhook kaydeder
    /// </summary>
    Task<(bool Success, string WebhookId, string Message)> SaveWebhookAsync(Dictionary<string, object> settings, string webhookType, string webhookUrl);

    /// <summary>
    /// Webhook'u siler
    /// </summary>
    Task<(bool Success, string Message)> DeleteWebhookAsync(Dictionary<string, object> settings, string webhookId);

    /// <summary>
    /// Sistemin ihtiyaç duyduğu webhook türlerini döner
    /// </summary>
    string[] GetRequiredWebhookTypes();

    /// <summary>
    /// Webhook URL'ini oluşturur
    /// </summary>
    string GenerateWebhookUrl(Guid companyId, string webhookType);

    /// <summary>
    /// Bu platform için webhook desteği var mı kontrol eder
    /// </summary>
    bool SupportsWebhooks { get; }
}
