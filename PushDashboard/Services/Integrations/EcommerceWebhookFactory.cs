using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Services.Integrations.Common;
using PushDashboard.Services.Integrations.Common.Models;

namespace PushDashboard.Services.Integrations;

/// <summary>
/// E-ticaret platformları için webhook servislerini yöneten factory
/// </summary>
public class EcommerceWebhookFactory : IEcommerceWebhookFactory
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EcommerceWebhookFactory> _logger;

    public EcommerceWebhookFactory(
        ApplicationDbContext context,
        IServiceProvider serviceProvider,
        ILogger<EcommerceWebhookFactory> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IEcommerceWebhookService?> GetWebhookServiceAsync(Guid companyId, int integrationId)
    {
        try
        {
            var companyIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.IntegrationId == integrationId &&
                                         ci.IsActive &&
                                         ci.IsConfigured &&
                                         ci.Integration.Type == "ecommerce");

            if (companyIntegration == null)
            {
                _logger.LogWarning("No active e-commerce integration found for company {CompanyId} and integration {IntegrationId}",
                    companyId, integrationId);
                return null;
            }

            return GetWebhookServiceByPlatformName(companyIntegration.Integration.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook service for company {CompanyId} and integration {IntegrationId}",
                companyId, integrationId);
            return null;
        }
    }

    public async Task<IEcommerceWebhookService?> GetWebhookServiceByCompanyAsync(Guid companyId)
    {
        try
        {
            var companyIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.IsActive &&
                                         ci.IsConfigured &&
                                         ci.Integration.Type == "ecommerce");

            if (companyIntegration == null)
            {
                _logger.LogWarning("No active e-commerce integration found for company {CompanyId}", companyId);
                return null;
            }

            return GetWebhookServiceByPlatformName(companyIntegration.Integration.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook service for company {CompanyId}", companyId);
            return null;
        }
    }

    public IEcommerceWebhookService? GetWebhookServiceByPlatformName(string platformName)
    {
        if (string.IsNullOrEmpty(platformName))
        {
            return null;
        }

        // Artık platform-specific webhook servisleri yok
        // Direkt IEcommerceService kullanıyoruz
        return platformName.ToLower() switch
        {
            "ticimax" => new EcommerceWebhookServiceAdapter(_serviceProvider.GetService<IEcommerceService>()!),
            // Gelecekte diğer platformlar buraya eklenecek
            // "ikas" => new EcommerceWebhookServiceAdapter(_serviceProvider.GetService<IEcommerceService>()!),
            // "ideasoft" => new EcommerceWebhookServiceAdapter(_serviceProvider.GetService<IEcommerceService>()!),
            // "tsoft" => new EcommerceWebhookServiceAdapter(_serviceProvider.GetService<IEcommerceService>()!),
            // "shopify" => new EcommerceWebhookServiceAdapter(_serviceProvider.GetService<IEcommerceService>()!),
            _ => null
        };
    }

    public async Task<(bool Success, string Message)> EnsureWebhooksForIntegrationAsync(Guid companyId, int integrationId, Dictionary<string, object> settings)
    {
        try
        {
            var webhookService = await GetWebhookServiceAsync(companyId, integrationId);

            if (webhookService == null)
            {
                return (false, "Bu e-ticaret platformu için webhook desteği bulunmuyor.");
            }

            if (!webhookService.SupportsWebhooks)
            {
                return (true, "Bu platform webhook desteği sunmuyor.");
            }

            return await webhookService.EnsureWebhooksAsync(companyId, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring webhooks for company {CompanyId} and integration {IntegrationId}",
                companyId, integrationId);
            return (false, "Webhook'lar kontrol edilirken hata oluştu: " + ex.Message);
        }
    }

    public string[] GetSupportedPlatforms()
    {
        return new[]
        {
            "Ticimax"
            // Gelecekte diğer platformlar buraya eklenecek
            // "IKAS",
            // "Ideasoft",
            // "Tsoft",
            // "Shopify"
        };
    }

    public bool IsPlatformSupported(string platformName)
    {
        if (string.IsNullOrEmpty(platformName))
        {
            return false;
        }

        var supportedPlatforms = GetSupportedPlatforms();
        return supportedPlatforms.Any(p => string.Equals(p, platformName, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Adapter to convert IEcommerceService to IEcommerceWebhookService
/// </summary>
public class EcommerceWebhookServiceAdapter : IEcommerceWebhookService
{
    private readonly IEcommerceService _ecommerceService;

    public EcommerceWebhookServiceAdapter(IEcommerceService ecommerceService)
    {
        _ecommerceService = ecommerceService;
    }

    public string PlatformName => _ecommerceService.PlatformName;

    public bool SupportsWebhooks => true;

    public async Task<(bool Success, string Message)> EnsureWebhooksAsync(Guid companyId, Dictionary<string, object> settings)
    {
        try
        {
            // 1. Gerekli webhook tiplerini al
            var requiredWebhookTypes = GetRequiredWebhookTypes();

            // 2. Mevcut webhook'ları al
            var existingWebhooks = await _ecommerceService.GetWebhooksAsync(companyId);
            var existingWebhookTypes = existingWebhooks
                .Select(w => w.EventType)
                .Where(eventType => !string.IsNullOrEmpty(eventType))
                .ToHashSet();

            // 3. Eksik webhook'ları belirle
            var missingWebhookTypes = requiredWebhookTypes
                .Where(type => !existingWebhookTypes.Contains(type))
                .ToArray();

            if (missingWebhookTypes.Length == 0)
            {
                return (true, $"Tüm gerekli webhook'lar mevcut. Toplam: {existingWebhooks.Length}");
            }

            // 4. Eksik webhook'ları ekle
            var addedCount = 0;
            var errors = new List<string>();

            foreach (var webhookType in missingWebhookTypes)
            {
                var webhookUrl = GenerateWebhookUrl(companyId, webhookType);

                var request = new EcommerceWebhookRequest
                {
                    EventType = webhookType,
                    Url = webhookUrl,
                    Username = "pushonica",
                    Password = "pushonica123!"
                };

                var result = await _ecommerceService.SaveWebhookAsync(companyId, request);

                if (result.Success)
                {
                    addedCount++;
                }
                else
                {
                    errors.Add($"{webhookType}: {result.Message}");
                }
            }

            // 5. Sonuç mesajı oluştur
            if (errors.Count == 0)
            {
                return (true, $"Başarıyla {addedCount} webhook eklendi. Toplam eksik: {missingWebhookTypes.Length}");
            }
            else if (addedCount > 0)
            {
                var errorMessage = string.Join(", ", errors);
                return (false, $"{addedCount} webhook eklendi, {errors.Count} hata: {errorMessage}");
            }
            else
            {
                var errorMessage = string.Join(", ", errors);
                return (false, $"Webhook eklenemedi. Hatalar: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Webhook ensure işlemi sırasında hata: {ex.Message}");
        }
    }

    public async Task<object[]> GetExistingWebhooksAsync(Dictionary<string, object> settings)
    {
        // Company ID'yi settings'ten al
        if (!settings.TryGetValue("companyId", out var companyIdObj) || !Guid.TryParse(companyIdObj.ToString(), out var companyId))
        {
            return new object[0];
        }

        var webhooks = await _ecommerceService.GetWebhooksAsync(companyId);
        return webhooks.Cast<object>().ToArray();
    }

    public async Task<(bool Success, string WebhookId, string Message)> SaveWebhookAsync(Dictionary<string, object> settings, string webhookType, string webhookUrl)
    {
        // Company ID'yi settings'ten al
        if (!settings.TryGetValue("companyId", out var companyIdObj) || !Guid.TryParse(companyIdObj.ToString(), out var companyId))
        {
            return (false, "", "Company ID not found in settings");
        }

        var request = new EcommerceWebhookRequest
        {
            EventType = webhookType,
            Url = webhookUrl,
            Username = "pushonica",
            Password = "pushonica123!"
        };

        var result = await _ecommerceService.SaveWebhookAsync(companyId, request);
        return (result.Success, result.WebhookId ?? "", result.Message);
    }

    public async Task<(bool Success, string Message)> DeleteWebhookAsync(Dictionary<string, object> settings, string webhookId)
    {
        // Company ID'yi settings'ten al
        if (!settings.TryGetValue("companyId", out var companyIdObj) || !Guid.TryParse(companyIdObj.ToString(), out var companyId))
        {
            return (false, "Company ID not found in settings");
        }

        var result = await _ecommerceService.DeleteWebhookAsync(companyId, webhookId);
        return (result.Success, result.Message);
    }

    public string[] GetRequiredWebhookTypes()
    {
        return new[] 
        { 
            nameof(EcommerceWebhookEventType.OrderCreated), 
            nameof(EcommerceWebhookEventType.OrderStatusChanged), 
            nameof(EcommerceWebhookEventType.CustomerCreated), 
            nameof(EcommerceWebhookEventType.CustomerUpdated),
            nameof(EcommerceWebhookEventType.CustomerFavoriteProductAdded),
            nameof(EcommerceWebhookEventType.CustomerFavoriteProductRemoved),
            nameof(EcommerceWebhookEventType.ProductUpdated),
            nameof(EcommerceWebhookEventType.ProductStockChanged)
        };
    }

    public string GenerateWebhookUrl(Guid companyId, string webhookType)
    {
        return $"https://admin.pushonica.com/webhook/{PlatformName.ToLower()}/{companyId}/{webhookType}";
    }
}
