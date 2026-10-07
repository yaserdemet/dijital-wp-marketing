using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Models;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.WhatsApp;
using PushDashboard.Exceptions;
using System.Text.Json;

namespace PushDashboard.Services;

/// <summary>
/// Service for managing integrations
/// </summary>
public class IntegrationService : IIntegrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEcommerceWebhookFactory _webhookFactory;
    private readonly IWhatsAppWebhookService _whatsAppWebhookService;
    private readonly ICustomerService _customerService;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationService> _logger;

    public IntegrationService(
        ApplicationDbContext context,
        IEcommerceWebhookFactory webhookFactory,
        IWhatsAppWebhookService whatsAppWebhookService,
        ICustomerService customerService,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<IntegrationService> logger)
    {
        _context = context;
        _webhookFactory = webhookFactory;
        _whatsAppWebhookService = whatsAppWebhookService;
        _customerService = customerService;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // === Integration Management ===

    public async Task<List<Integration>> GetAllActiveIntegrationsAsync()
    {
        return await _context.Integrations
            .Where(i => i.IsActive)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.SortOrder)
            .ToListAsync();
    }

    public async Task<List<GroupedIntegrationsDto>> GetGroupedIntegrationsAsync(Guid companyId)
    {
        var integrations = await GetAllActiveIntegrationsAsync();
        var companyIntegrations = await GetCompanyIntegrationsAsync(companyId);

        var companyIntegrationDict = companyIntegrations.ToDictionary(ci => ci.IntegrationId, ci => ci);

        var grouped = integrations
            .GroupBy(i => i.Category)
            .Select(g => new GroupedIntegrationsDto
            {
                Category = g.Key,
                Integrations = g.Select(i =>
                {
                    var isAdded = companyIntegrationDict.TryGetValue(i.Id, out var ci);
                    return new IntegrationWithStatusDto
                    {
                        Id = i.Id,
                        Name = i.Name,
                        Description = i.Description ?? string.Empty,
                        IconClass = i.IconClass ?? string.Empty,
                        IconColor = i.IconColor ?? string.Empty,
                        Type = i.Type,
                        IsAdded = isAdded,
                        IsActive = isAdded && ci!.IsActive,
                        IsConfigured = isAdded && ci!.IsConfigured,
                        LastSyncAt = isAdded ? ci!.LastSyncAt : null
                    };
                }).ToList()
            })
            .ToList();

        return grouped;
    }

    public async Task<List<CompanyIntegration>> GetCompanyIntegrationsAsync(Guid companyId)
    {
        return await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .Where(ci => ci.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<CompanyIntegration?> GetCompanyIntegrationAsync(Guid companyId, int integrationId)
    {
        return await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .FirstOrDefaultAsync(ci => ci.CompanyId == companyId && ci.IntegrationId == integrationId);
    }

    public async Task<CompanyIntegration> AddIntegrationAsync(Guid companyId, int integrationId)
    {
        // Check if integration exists
        var integration = await _context.Integrations
            .FirstOrDefaultAsync(i => i.Id == integrationId && i.IsActive);

        if (integration == null)
        {
            throw new NotFoundException("Integration", integrationId);
        }

        // Check if company already has this integration
        var existingCompanyIntegration = await _context.CompanyIntegrations
            .FirstOrDefaultAsync(ci => ci.CompanyId == companyId && ci.IntegrationId == integrationId);

        if (existingCompanyIntegration != null)
        {
            throw new DuplicateException($"{integration.Name} entegrasyonu zaten şirketiniz için eklenmiş.");
        }

        // Create new company integration
        var companyIntegration = new CompanyIntegration
        {
            CompanyId = companyId,
            IntegrationId = integrationId,
            IsActive = true,
            IsConfigured = false,
            CreatedAt = DateTime.Now,
            SettingsJson = integration.DefaultSettingsTemplate
        };

        _context.CompanyIntegrations.Add(companyIntegration);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Integration {IntegrationName} added for company {CompanyId}",
            integration.Name, companyId);

        return companyIntegration;
    }

    // === Settings Management ===

    public async Task<Dictionary<string, object>> GetIntegrationSettingsAsync(
        Guid companyId,
        int integrationId)
    {
        var companyIntegration = await GetCompanyIntegrationAsync(companyId, integrationId);

        if (companyIntegration == null)
        {
            throw new NotFoundException("Company integration", integrationId);
        }

        var settings = string.IsNullOrEmpty(companyIntegration.SettingsJson)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(companyIntegration.SettingsJson)
                ?? new Dictionary<string, object>();

        if (companyIntegration.Integration.Name.Equals("Ticimax", StringComparison.OrdinalIgnoreCase) ||
            companyIntegration.Integration.Name.Equals("IKAS", StringComparison.OrdinalIgnoreCase) ||
            companyIntegration.Integration.Name.Equals("Ideasoft", StringComparison.OrdinalIgnoreCase) ||
            companyIntegration.Integration.Name.Equals("Tsoft", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(companyIntegration.KvkkLink))
            {
                settings["kvkkLink"] = companyIntegration.KvkkLink;
            }
            if (!string.IsNullOrWhiteSpace(companyIntegration.TicariElektronikIletiLink))
            {
                settings["ticariElektronikIletiLink"] = companyIntegration.TicariElektronikIletiLink;
            }
            settings["legalTextsAccepted"] = companyIntegration.LegalTextsAccepted;
        }

        return settings;
    }

    public async Task<string> SaveIntegrationSettingsAsync(
        Guid companyId,
        int integrationId,
        Dictionary<string, object> settings,
        string userId,
        string scheme,
        string host)
    {
        var companyIntegration = await GetCompanyIntegrationAsync(companyId, integrationId);

        if (companyIntegration == null)
        {
            throw new NotFoundException("Company integration", integrationId);
        }

        if (companyIntegration.Integration.Name.Equals("Ticimax", StringComparison.OrdinalIgnoreCase) ||
            companyIntegration.Integration.Name.Equals("IKAS", StringComparison.OrdinalIgnoreCase) ||
            companyIntegration.Integration.Name.Equals("Ideasoft", StringComparison.OrdinalIgnoreCase) ||
            companyIntegration.Integration.Name.Equals("Tsoft", StringComparison.OrdinalIgnoreCase))
        {
            var kvkkLink = settings.GetValueOrDefault("kvkkLink")?.ToString();
            var ticariElektronikIletiLink = settings.GetValueOrDefault("ticariElektronikIletiLink")?.ToString();
            var legalTextsAccepted = settings.GetValueOrDefault("legalTextsAccepted");

            if (string.IsNullOrWhiteSpace(kvkkLink))
            {
                throw new ValidationException("KVKK Aydınlatma Metni Linki boş bırakılamaz. Bu alan entegrasyonun tamamlanması için zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(ticariElektronikIletiLink))
            {
                throw new ValidationException("Ticari Elektronik İleti Metni Linki boş bırakılamaz. Bu alan entegrasyonun tamamlanması için zorunludur.");
            }

            bool isAccepted = false;
            if (legalTextsAccepted is bool boolValue)
            {
                isAccepted = boolValue;
            }
            else if (legalTextsAccepted is string stringValue)
            {
                bool.TryParse(stringValue, out isAccepted);
            }
            else if (legalTextsAccepted is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.True)
                    isAccepted = true;
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.False)
                    isAccepted = false;
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    bool.TryParse(jsonElement.GetString(), out isAccepted);
            }

            if (!isAccepted)
            {
                throw new ValidationException("ETK ve KVKK metinlerinin eksiksiz ve mevzuata uygun şekilde doldurulması müşterinin sorumluluğundadır. Devam etmek için sorumluluk bildirimini onaylamanız gerekmektedir.");
            }

            companyIntegration.KvkkLink = kvkkLink.Trim();
            companyIntegration.TicariElektronikIletiLink = ticariElektronikIletiLink.Trim();
            companyIntegration.LegalTextsAccepted = true;

            settings.Remove("kvkkLink");
            settings.Remove("ticariElektronikIletiLink");
            settings.Remove("legalTextsAccepted");
        }

        // Mevcut ayarları al
        var currentSettings = string.IsNullOrEmpty(companyIntegration.SettingsJson)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(companyIntegration.SettingsJson)
                ?? new Dictionary<string, object>();

        // Ayarların değişip değişmediğini kontrol et
        bool settingsChanged = !AreSettingsEqual(currentSettings, settings);

        // Yeni ayarları kaydet
        companyIntegration.SettingsJson = JsonSerializer.Serialize(settings);
        companyIntegration.Settings = settings;

        if (!companyIntegration.IsConfigured)
        {
            companyIntegration.IsConfigured = true;
            companyIntegration.ConfiguredAt = DateTime.Now;
            settingsChanged = true;
        }

        // IsNew flagini ayarla
        if (settingsChanged)
        {
            companyIntegration.IsNew = true;
            _logger.LogInformation("Integration settings changed for company {CompanyId}. IsNew set to true.", companyId);
        }

        await _context.SaveChangesAsync();

        // E-ticaret entegrasyonu ise webhook'ları kontrol et ve ekle
        string webhookMessage = "";
        if (companyIntegration.Integration.Type == "ecommerce")
        {
            try
            {
                var (webhookSuccess, webhookMsg) = await _webhookFactory.EnsureWebhooksForIntegrationAsync(
                    companyId,
                    integrationId,
                    settings);

                webhookMessage = webhookSuccess
                    ? $" Webhook'lar: {webhookMsg}"
                    : $" Webhook uyarısı: {webhookMsg}";

                _logger.LogInformation("Webhook ensure completed for company {CompanyId}: {Message}",
                    companyId, webhookMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring webhooks for company {CompanyId}", companyId);
                webhookMessage = " Webhook kontrolü sırasında bir hata oluştu.";
            }
        }
        // WhatsApp entegrasyonu ise Gupshup webhook'unu kaydet
        else if (companyIntegration.Integration.Name == "WhatsApp")
        {
            try
            {
                // WhatsApp ayarlarından appId ve partnerToken'ı al
                if (settings.TryGetValue("appId", out var appIdObj) &&
                    settings.TryGetValue("token", out var partnerTokenObj))
                {
                    var appId = appIdObj?.ToString();
                    var partnerToken = partnerTokenObj?.ToString();

                    if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(partnerToken))
                    {
                        var result = await _whatsAppWebhookService.RegisterWebhookAsync(
                            appId,
                            partnerToken,
                            companyId);

                        webhookMessage = result.Success
                            ? $" Webhook kaydedildi: {result.WebhookUrl}"
                            : $" Webhook uyarısı: {result.Message}";

                        _logger.LogInformation(
                            "WhatsApp webhook registration completed for company {CompanyId}: {Success} - {Message}",
                            companyId,
                            result.Success,
                            result.Message);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "WhatsApp integration appId or partnerToken missing for company {CompanyId}",
                            companyId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering WhatsApp webhook for company {CompanyId}", companyId);
                webhookMessage = " Webhook kaydedilemedi, ancak ayarlar kaydedildi.";
            }
        }

        return $"Ayarlar başarıyla kaydedildi.{webhookMessage}";
    }

    private bool AreSettingsEqual(Dictionary<string, object> settings1, Dictionary<string, object> settings2)
    {
        var json1 = JsonSerializer.Serialize(settings1);
        var json2 = JsonSerializer.Serialize(settings2);
        return json1 == json2;
    }

    // === Connection Testing ===

    public async Task<ConnectionTestResult> TestConnectionAsync(
        int integrationId,
        Dictionary<string, object> settings,
        Guid? companyId = null)
    {
        // Find integration
        var integration = await _context.Integrations
            .FirstOrDefaultAsync(i => i.Id == integrationId);

        if (integration == null)
        {
            return ConnectionTestResult.CreateFailure(
                "Entegrasyon bulunamadı.",
                "INTEGRATION_NOT_FOUND");
        }

        // Test by integration type
        var result = await TestConnectionByIntegrationType(integration.Name, settings);

        // If companyId is provided and test failed, mark integration as deactivated by service
        if (companyId.HasValue && !result.Success)
        {
            var companyIntegration = await _context.CompanyIntegrations
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId.Value && ci.IntegrationId == integrationId);

            if (companyIntegration != null)
            {
                companyIntegration.IsActive = false;
                companyIntegration.DeactivatedByService = true;
                companyIntegration.DeactivatedAt = DateTime.Now;
                companyIntegration.DeactivationReason = result.Message ?? "Bağlantı testi başarısız oldu.";
                await _context.SaveChangesAsync();

                _logger.LogWarning("Integration {IntegrationId} deactivated by service for company {CompanyId}. Reason: {Reason}",
                    integrationId, companyId.Value, result.Message);
            }
        }
        // If test succeeded and it was previously deactivated by service, reactivate it
        else if (companyId.HasValue && result.Success)
        {
            var companyIntegration = await _context.CompanyIntegrations
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId.Value && ci.IntegrationId == integrationId);

            if (companyIntegration != null && companyIntegration.DeactivatedByService)
            {
                companyIntegration.IsActive = true;
                companyIntegration.DeactivatedByService = false;
                companyIntegration.DeactivatedAt = null;
                companyIntegration.DeactivationReason = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Integration {IntegrationId} reactivated for company {CompanyId}.",
                    integrationId, companyId.Value);
            }
        }

        return result;
    }

    private async Task<ConnectionTestResult> TestConnectionByIntegrationType(
        string integrationType,
        Dictionary<string, object> settings)
    {
        try
        {
            return integrationType.ToLower() switch
            {
                "ticimax" => await TestTicimaxConnection(settings),
                "ikas" => await TestGenericUrlConnection(settings),
                "ideasoft" => await TestGenericUrlConnection(settings),
                "tsoft" => await TestGenericUrlConnection(settings),
                "email smtp" => await TestSmtpConnection(settings),
                "whatsapp" => await TestWhatsAppConnection(settings),
                "sms" => await TestSmsConnection(settings),
                _ => new ConnectionTestResult
                {
                    Success = false,
                    Message = $"'{integrationType}' entegrasyonu için test desteği henüz eklenmemiş.",
                    ErrorCode = "UNSUPPORTED_INTEGRATION"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection for integration type {IntegrationType}",
                integrationType);
            return new ConnectionTestResult
            {
                Success = false,
                Message = "Bağlantı test edilirken hata oluştu: " + ex.Message,
                ErrorCode = "TEST_ERROR"
            };
        }
    }

    private Task<ConnectionTestResult> TestGenericUrlConnection(Dictionary<string, object> settings)
    {
        var apiUrl = settings.GetValueOrDefault("apiUrl")?.ToString();

        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            return Task.FromResult(new ConnectionTestResult
            {
                Success = false,
                Message = "Site adresi boş bırakılamaz. Lütfen geçerli bir URL girin.",
                ErrorCode = "MISSING_API_URL"
            });
        }

        // Sadece URL formatını kontrol et, bağlantı testi yapma
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var validatedUri) || (validatedUri.Scheme != Uri.UriSchemeHttp && validatedUri.Scheme != Uri.UriSchemeHttps))
        {
            return Task.FromResult(new ConnectionTestResult
            {
                Success = false,
                Message = "Girilen adres geçerli bir URL formatında değil. Lütfen 'http://' veya 'https://' ile başlayan tam adresi girin.",
                ErrorCode = "INVALID_URL_FORMAT"
            });
        }

        return Task.FromResult(new ConnectionTestResult
        {
            Success = true,
            Message = "Bağlantı ayarları geçerli görünüyor.", // Pasif kontrol mesajı
            ErrorCode = "SUCCESS",
            ResponseTime = TimeSpan.Zero
        });
    }

    private async Task<ConnectionTestResult> TestTicimaxConnection(Dictionary<string, object> settings)
    {
        try
        {
            var apiUrl = settings.GetValueOrDefault("apiUrl")?.ToString();
            var apiKey = settings.GetValueOrDefault("apiKey")?.ToString();

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = "Site adresi boş bırakılamaz. Lütfen Ticimax site adresinizi girin (örn: https://siteniz.ticimax.com).",
                    ErrorCode = "MISSING_API_URL"
                };
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = "API anahtarı boş bırakılamaz. Lütfen Ticimax panelinizden API anahtarınızı kopyalayın.",
                    ErrorCode = "MISSING_API_KEY"
                };
            }

            var ticimaxService = _serviceProvider.GetService<PushDashboard.Services.Integrations.Ticimax.TicimaxService>();
            if (ticimaxService == null)
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Message = "Ticimax servisi şu an kullanılamıyor. Lütfen daha sonra tekrar deneyin.",
                    ErrorCode = "SERVICE_NOT_FOUND"
                };
            }

            var result = await ticimaxService.TestConnectionAsync(apiUrl, apiKey);

            return new ConnectionTestResult
            {
                Success = result.Success,
                Message = result.Message,
                ErrorCode = result.ErrorCode,
                AdditionalInfo = result.ConnectionInfo,
                ResponseTime = TimeSpan.FromMilliseconds(100)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Ticimax connection");
            return new ConnectionTestResult
            {
                Success = false,
                Message = "Bağlantı test edilirken bir hata oluştu. Lütfen bilgilerinizi kontrol edip tekrar deneyin.",
                ErrorCode = "TICIMAX_TEST_ERROR"
            };
        }
    }

    private async Task<ConnectionTestResult> TestSmtpConnection(Dictionary<string, object> settings)
    {
        try
        {
            var emailService = _serviceProvider.GetService<PushDashboard.Services.Notifications.EmailNotificationChannelService>();
            if (emailService == null)
            {
                return ConnectionTestResult.CreateFailure(
                    "Email notification servisi bulunamadı.",
                    "SERVICE_NOT_FOUND",
                    "Email SMTP");
            }

            // Note: This requires companyId from context - will need to be passed or handled differently
            // For now we'll return a placeholder
            return ConnectionTestResult.CreateSuccess(
                "SMTP bağlantısı test edilemedi - company context gerekli.",
                "Email SMTP");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMTP connection");
            return ConnectionTestResult.CreateError(ex, "Email SMTP");
        }
    }

    private async Task<ConnectionTestResult> TestWhatsAppConnection(Dictionary<string, object> settings)
    {
        try
        {
            var whatsAppService = _serviceProvider.GetService<PushDashboard.Services.Notifications.WhatsAppNotificationChannelService>();
            if (whatsAppService == null)
            {
                return ConnectionTestResult.CreateFailure(
                    "WhatsApp notification servisi bulunamadı.",
                    "SERVICE_NOT_FOUND",
                    "WhatsApp");
            }

            // Test using the service's test method
            var result = await whatsAppService.TestConnectionWithSettingsAsync(Guid.Empty, settings);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing WhatsApp connection");
            return ConnectionTestResult.CreateError(ex, "WhatsApp");
        }
    }

    private async Task<ConnectionTestResult> TestSmsConnection(Dictionary<string, object> settings)
    {
        try
        {
            var smsService = _serviceProvider.GetService<PushDashboard.Services.Notifications.SmsNotificationChannelService>();
            if (smsService == null)
            {
                return ConnectionTestResult.CreateFailure(
                    "SMS notification servisi bulunamadı.",
                    "SERVICE_NOT_FOUND",
                    "SMS");
            }

            // Test using the service's test method
            // Note: We're passing Guid.Empty as we don't have company context here
            // The service will validate based on settings alone
            var result = await smsService.TestConnectionWithSettingsAsync(Guid.Empty, settings);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMS connection");
            return ConnectionTestResult.CreateError(ex, "SMS");
        }
    }

    // === Status Mappings ===

    public async Task<bool> HasOrderStatusModuleAsync(Guid companyId)
    {
        return await _context.CompanyModules
            .Include(cm => cm.Module)
            .AnyAsync(cm => cm.CompanyId == companyId &&
                          cm.Module.Name == "Sipariş Durumu Bildirimleri" &&
                          cm.IsActive &&
                          (!cm.ExpiresAt.HasValue || cm.ExpiresAt.Value > DateTime.UtcNow));
    }

    public async Task<Dictionary<string, string>> GetStatusMappingsAsync(
        Guid companyId,
        int integrationId)
    {
        var companyIntegration = await GetCompanyIntegrationAsync(companyId, integrationId);

        if (companyIntegration == null)
        {
            throw new NotFoundException("Company integration", integrationId);
        }

        if (companyIntegration.Settings == null ||
            !companyIntegration.Settings.ContainsKey("statusMappings"))
        {
            // Return default mappings for Ticimax
            if (companyIntegration.Integration.Name.ToLower() == "ticimax")
            {
                return GetDefaultTicimaxMappings();
            }

            return new Dictionary<string, string>();
        }

        var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(
            companyIntegration.Settings["statusMappings"].ToString() ?? "{}");

        return mappings ?? new Dictionary<string, string>();
    }

    public async Task SaveStatusMappingsAsync(
        Guid companyId,
        int integrationId,
        Dictionary<string, string> mappings,
        string userId)
    {
        var hasModule = await HasOrderStatusModuleAsync(companyId);
        if (!hasModule)
        {
            throw new ValidationException(
                "Sipariş Durumu Bildirimleri modülüne sahip değilsiniz. Lütfen modülü aktif hale getirin.");
        }

        var companyIntegration = await GetCompanyIntegrationAsync(companyId, integrationId);

        if (companyIntegration == null)
        {
            throw new NotFoundException("Company integration", integrationId);
        }

        // Update settings
        var settings = companyIntegration.Settings ?? new Dictionary<string, object>();
        settings["statusMappings"] = mappings;

        companyIntegration.SettingsJson = JsonSerializer.Serialize(settings);
        companyIntegration.Settings = settings;
        companyIntegration.ConfiguredAt = DateTime.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Status mappings saved for company {CompanyId}, integration {IntegrationId}",
            companyId, integrationId);
    }

    public Dictionary<string, string> GetDefaultTicimaxMappings()
    {
        return new Dictionary<string, string>
        {
            // Ticimax order status IDs -> Internal status names
            ["1"] = "Beklemede",
            ["2"] = "Onaylandı",
            ["3"] = "Hazırlanıyor",
            ["4"] = "Kargoya Verildi",
            ["5"] = "Tamamlandı",
            ["6"] = "İptal Edildi",
            ["7"] = "İade",
            ["8"] = "Ödeme Bekliyor",
            ["9"] = "Ödeme Alındı",
            ["10"] = "Ödeme Başarısız"
        };
    }

    // === Synchronization ===

    public async Task<SyncResultData> SyncIntegrationAsync(
        Guid companyId,
        int integrationId)
    {
        var companyIntegration = await GetCompanyIntegrationAsync(companyId, integrationId);

        if (companyIntegration == null)
        {
            throw new NotFoundException("Company integration", integrationId);
        }

        if (!companyIntegration.IsConfigured)
        {
            throw new ValidationException(
                "Entegrasyonun ayarları tamamlanmamış. Lütfen önce ayarları yapın.");
        }

        var integration = companyIntegration.Integration;
        var integrationName = integration.Name;
        var syncStartTime = DateTime.Now;

        // Test connection before sync - only for supported integration types
        var settings = companyIntegration.Settings;
        var supportedTestTypes = new[] { "ticimax", "email smtp", "whatsapp", "sms" };

        if (supportedTestTypes.Contains(integrationName.ToLower()))
        {
            var testResult = await TestConnectionByIntegrationType(integrationName, settings);

            if (!testResult.Success)
            {
                // Connection test failed - deactivate integration by service
                companyIntegration.IsActive = false;
                companyIntegration.DeactivatedByService = true;
                companyIntegration.DeactivatedAt = DateTime.Now;
                companyIntegration.DeactivationReason = testResult.Message ?? "Bağlantı testi başarısız oldu.";
                await _context.SaveChangesAsync();

                _logger.LogWarning("Integration {IntegrationName} deactivated by service for company {CompanyId}. Reason: {Reason}",
                    integrationName, companyId, testResult.Message);

                return new SyncResultData
                {
                    Success = false,
                    Message = $"Bağlantı testi başarısız: {testResult.Message}",
                    SyncDuration = $"{(DateTime.Now - syncStartTime).TotalSeconds:F1}s",
                    LastSyncAt = companyIntegration.LastSyncAt
                };
            }

            // Connection successful - clear any previous deactivation flags
            if (companyIntegration.DeactivatedByService)
            {
                companyIntegration.DeactivatedByService = false;
                companyIntegration.DeactivatedAt = null;
                companyIntegration.DeactivationReason = null;
            }
        }

        // E-ticaret entegrasyonu ise sepet ve müşteri senkronizasyonu yap
        if (integration.Type == "ecommerce")
        {
            var result = await _customerService.SyncCustomersAndBasketsAsync(companyId);

            // Update last sync time
            companyIntegration.IsActive = true;
            companyIntegration.DeactivatedByService = false;
            companyIntegration.LastSyncAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return new SyncResultData
            {
                Success = true,
                Message = result.Message,
                SyncedCustomers = result.SyncedCustomers,
                SyncedCarts = result.SyncedCarts,
                SyncDuration = result.SyncDuration,
                LastSyncAt = companyIntegration.LastSyncAt
            };
        }
        else
        {
            // Diğer entegrasyon tipleri için
            var syncDuration = DateTime.Now - syncStartTime;
            companyIntegration.LastSyncAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return new SyncResultData
            {
                Success = true,
                Message = $"{integrationName} senkronizasyonu başarıyla tamamlandı.",
                SyncDuration = $"{syncDuration.TotalSeconds:F1}s",
                LastSyncAt = companyIntegration.LastSyncAt
            };
        }
    }

    // === Utility ===

    public async Task<List<IntegrationDto>> GetActiveCompanyIntegrationsAsync(Guid companyId)
    {
        var companyIntegrations = await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .Where(ci => ci.CompanyId == companyId && ci.IsActive && ci.IsConfigured)
            .ToListAsync();

        return companyIntegrations
            .Select(ci => new IntegrationDto
            {
                Id = ci.IntegrationId,
                Name = ci.Integration.Name,
                Type = ci.Integration.Type,
                Description = ci.Integration.Description,
                IsActive = ci.IsActive,
                IsConfigured = ci.IsConfigured,
                LastSyncAt = ci.LastSyncAt
            })
            .ToList();
    }

    public async Task<string> GenerateModuleEmbedCodeAsync(Guid companyId)
    {
        // Get company
        var company = await _context.Companies.FindAsync(companyId);
        if (company == null)
        {
            return "<!-- Company not found -->";
        }

        var embedCode = $@"<!-- Pushonica Modules -->
<script
src=""https://assets.pushonica.com/pushonica-modules.js""
data-module-id=""{companyId}""
async>
</script>
<!-- End Pushonica Modules -->";

        return embedCode.Trim();
    }
}
