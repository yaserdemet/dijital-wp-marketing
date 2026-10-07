namespace PushDashboard.Services.Integrations;

/// <summary>
/// E-ticaret webhook servislerini yöneten factory interface'i
/// </summary>
public interface IEcommerceWebhookFactory
{
    /// <summary>
    /// Belirli bir şirket ve entegrasyon için webhook servisi döner
    /// </summary>
    Task<IEcommerceWebhookService?> GetWebhookServiceAsync(Guid companyId, int integrationId);

    /// <summary>
    /// Şirketin aktif e-ticaret entegrasyonu için webhook servisi döner
    /// </summary>
    Task<IEcommerceWebhookService?> GetWebhookServiceByCompanyAsync(Guid companyId);

    /// <summary>
    /// Platform adına göre webhook servisi döner
    /// </summary>
    IEcommerceWebhookService? GetWebhookServiceByPlatformName(string platformName);

    /// <summary>
    /// Belirli bir entegrasyon için webhook'ları kontrol eder ve ekler
    /// </summary>
    Task<(bool Success, string Message)> EnsureWebhooksForIntegrationAsync(Guid companyId, int integrationId, Dictionary<string, object> settings);

    /// <summary>
    /// Desteklenen platform isimlerini döner
    /// </summary>
    string[] GetSupportedPlatforms();

    /// <summary>
    /// Platform desteklenip desteklenmediğini kontrol eder
    /// </summary>
    bool IsPlatformSupported(string platformName);
}
