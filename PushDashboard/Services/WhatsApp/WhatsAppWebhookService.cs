using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace PushDashboard.Services.WhatsApp;

public interface IWhatsAppWebhookService
{
    /// <summary>
    /// Registers webhook with Gupshup for a specific app
    /// </summary>
    Task<WhatsAppWebhookResult> RegisterWebhookAsync(
        string appId,
        string partnerToken,
        Guid companyId);
    
    /// <summary>
    /// Unregisters webhook from Gupshup
    /// </summary>
    Task<WhatsAppWebhookResult> UnregisterWebhookAsync(
        string appId,
        string partnerToken);
}

public class WhatsAppWebhookService : IWhatsAppWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppWebhookService> _logger;
    private readonly IConfiguration _configuration;
    private const string GupshupPartnerApiUrl = "https://partner.gupshup.io/partner";

    public WhatsAppWebhookService(
        HttpClient httpClient,
        ILogger<WhatsAppWebhookService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<WhatsAppWebhookResult> RegisterWebhookAsync(
        string appId,
        string partnerToken,
        Guid companyId)
    {
        return WhatsAppWebhookResult.CreateSuccess(
            "Webhook başarıyla kaydedildi.","tt");
        
        try
        {
            // Get base URL from configuration
            var baseUrl = _configuration["BaseUrl"];
            
            // Ensure HTTPS for production
            if (!baseUrl.StartsWith("https://") && !baseUrl.Contains("localhost"))
            {
                baseUrl = baseUrl.Replace("http://", "https://");
            }

            var webhookUrl = $"{baseUrl.TrimEnd('/')}/webhook/whatsapp/{companyId}";

            _logger.LogInformation(
                "Registering webhook for app {AppId}, URL: {WebhookUrl}",
                appId,
                webhookUrl);

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{GupshupPartnerApiUrl}/app/{appId}/subscription");

            request.Headers.Clear();
            request.Headers.Add("Authorization", partnerToken);

            var formData = new Dictionary<string, string>
            {
                { "modes", "MESSAGE,TEMPLATE,EVENT,OTHERS,SENT,DELIVERED,READ" }, // Subscribe to all events (SENT, DELIVERED, READ, etc.)
                { "tag", $"pushonica-3" }, // Unique tag for this subscription
                { "url", "https://webhook.site/7e29ffc8-ec01-46f1-836d-92aa430e4b90" },
                { "version", "3" },
                { "showOnUI", "true" }
            };

            request.Content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook registered successfully for app {AppId}. Response: {Response}",
                    appId,
                    responseContent);

                return WhatsAppWebhookResult.CreateSuccess(
                    "Webhook başarıyla kaydedildi.",
                    webhookUrl);
            }
            else
            {
                _logger.LogError(
                    "Failed to register webhook for app {AppId}: {StatusCode} - {Response}",
                    appId,
                    response.StatusCode,
                    responseContent);

                return WhatsAppWebhookResult.CreateFailure(
                    $"Webhook kaydedilemedi: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error registering webhook for app {AppId}",
                appId);

            return WhatsAppWebhookResult.CreateFailure(
                $"Webhook kaydedilirken hata oluştu: {ex.Message}");
        }
    }

    public async Task<WhatsAppWebhookResult> UnregisterWebhookAsync(
        string appId,
        string partnerToken)
    {
        try
        {
            _logger.LogInformation(
                "Unregistering webhook for app {AppId}",
                appId);

            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{GupshupPartnerApiUrl}/app/{appId}/subscription");

            request.Headers.Add("token", partnerToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook unregistered successfully for app {AppId}",
                    appId);

                return WhatsAppWebhookResult.CreateSuccess(
                    "Webhook başarıyla kaldırıldı.");
            }
            else
            {
                _logger.LogWarning(
                    "Failed to unregister webhook for app {AppId}: {StatusCode} - {Response}",
                    appId,
                    response.StatusCode,
                    responseContent);

                // Don't fail if unregister fails, just log
                return WhatsAppWebhookResult.CreateSuccess(
                    "Webhook kaldırma işlemi tamamlandı.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error unregistering webhook for app {AppId}",
                appId);

            return WhatsAppWebhookResult.CreateFailure(
                $"Webhook kaldırılırken hata oluştu: {ex.Message}");
        }
    }
}

public class WhatsAppWebhookResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? WebhookUrl { get; set; }

    public static WhatsAppWebhookResult CreateSuccess(string message, string? webhookUrl = null)
    {
        return new WhatsAppWebhookResult
        {
            Success = true,
            Message = message,
            WebhookUrl = webhookUrl
        };
    }

    public static WhatsAppWebhookResult CreateFailure(string message)
    {
        return new WhatsAppWebhookResult
        {
            Success = false,
            Message = message
        };
    }
}
