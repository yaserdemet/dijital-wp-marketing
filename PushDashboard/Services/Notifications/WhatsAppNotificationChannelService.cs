using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.WhatsApp;

namespace PushDashboard.Services.Notifications;

public class WhatsAppNotificationChannelService : INotificationChannelService, IWhatsAppTemplateComponentService
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IWhatsAppTemplateService _templateService;
    private readonly ILogger<WhatsAppNotificationChannelService> _logger;

    public string ChannelType => "whatsapp";

    public WhatsAppNotificationChannelService(
        ApplicationDbContext context,
        IWhatsAppService whatsAppService,
        IWhatsAppTemplateService templateService,
        ILogger<WhatsAppNotificationChannelService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<bool> SendNotificationAsync(Guid companyId, Customer customer, string templateName, Dictionary<string, string> variables)
    {
        try
        {
            // WhatsApp entegrasyonunu kontrol et
            if (!await IsConfiguredAsync(companyId))
            {
                _logger.LogWarning("WhatsApp integration not configured for company {CompanyId}", companyId);
                return false;
            }

            // Müşterinin SMS/WhatsApp izni ve telefon numarası kontrolü
            var phoneNumber = customer.MobilePhone ?? customer.Phone;
            if (!customer.SmsPermission || string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogInformation("Customer {CustomerId} cannot receive WhatsApp. Permission: {HasPermission}, HasPhone: {HasPhone}",
                    customer.Id,
                    customer.SmsPermission,
                    !string.IsNullOrWhiteSpace(phoneNumber));

                return false;
            }

            // WhatsApp ayarlarını al
            var settings = await GetWhatsAppSettingsAsync(companyId);
            if (settings == null)
            {
                _logger.LogError("WhatsApp settings not found for company {CompanyId}", companyId);
                return false;
            }

            // Facebook'tan onaylanmış şablonları al
            var facebookTemplates = await _templateService.GetFacebookTemplatesAsync(companyId);

            // Template'i isim veya ID ile ara
            var template = facebookTemplates.FirstOrDefault(t =>
                (t.Name == templateName || t.Id == templateName) &&
                t.Status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase));

            if (template == null)
            {
                _logger.LogWarning("Approved WhatsApp template '{TemplateName}' not found for company {CompanyId}", templateName, companyId);
                return false;
            }

            // Template components'lerini hazırla (variable mappings kullanarak)
            var templateComponents = await PrepareWhatsAppTemplateComponentsAsync(companyId, template, _context, variables);

            // WhatsApp template mesajını gönder
            var request = new WhatsAppTemplateMessageRequest
            {
                ToPhoneNumber = phoneNumber,
                TemplateName = template.Name,
                TemplateLanguage = template.Language ?? "tr",
                Components = templateComponents,
                ConnectionSettings = settings
            };

            var result = await _whatsAppService.SendTemplateMessageAsync(request);

            if (result.Success)
            {
                _logger.LogInformation("WhatsApp notification sent successfully to customer {CustomerId}", customer.Id);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send WhatsApp notification to customer {CustomerId}: {Error}",
                    customer.Id,
                    result.Message);

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp notification to customer {CustomerId}", customer.Id);
            return false;
        }
    }

    public async Task<List<WhatsAppTemplateComponent>> PrepareWhatsAppTemplateComponentsAsync(
        Guid companyId,
        FacebookMessageTemplate template,
        ApplicationDbContext context,
        Dictionary<string, string> variables)
    {
        var components = new List<WhatsAppTemplateComponent>();

        if (template.Components == null) return components;

        // Get variable mappings for this template - use the provided context
        var mappings = await context.WhatsAppTemplateVariableMappings
            .Where(m => m.CompanyId == companyId && m.WhatsAppTemplateName == template.Name)
            .OrderBy(m => m.ComponentType)
            .ThenBy(m => m.VariablePosition)
            .ToListAsync();

        foreach (var component in template.Components)
        {
            var templateComponent = new WhatsAppTemplateComponent { Type = component.Type };
            var parameters = new List<WhatsAppTemplateParameter>();

            if (component.Type == "BODY")
            {
                // Get mappings for BODY component
                var bodyMappings = mappings
                    .Where(m => m.ComponentType == "BODY")
                    .OrderBy(m => m.VariablePosition)
                    .ToList();

                // Create parameters based on mappings
                foreach (var mapping in bodyMappings)
                {
                    var value = variables[mapping.VariableType];
                    if (string.IsNullOrWhiteSpace(value))
                        value = "-";
                    parameters.Add(new WhatsAppTemplateParameter
                    {
                        Type = "text",
                        Text = value
                    });
                }
            }
            else if (component.Type == "HEADER")
            {
                var headerFormat = component.Format?.ToUpperInvariant();
                var isMediaHeader = headerFormat is "IMAGE" or "VIDEO" or "DOCUMENT";

                if (isMediaHeader)
                {
                    // For media headers, add a media-type parameter with the media URL
                    // The URL could come from example data or a stored URL on the component
                    var mediaUrl = component.Example?.HeaderHandle?.FirstOrDefault() ?? component.Text;
                    
                    // Check for special media_url mapping saved during creation
                    var mediaMapping = mappings.FirstOrDefault(m => m.ComponentType == "HEADER" && m.VariableType == "media_url");
                    if (mediaMapping != null && !string.IsNullOrEmpty(mediaMapping.VariableLabel))
                    {
                        mediaUrl = mediaMapping.VariableLabel;
                    }

                    if (!string.IsNullOrEmpty(mediaUrl))
                    {
                        var mediaType = headerFormat!.ToLower();
                        if (mediaType == "ımage")
                            mediaType = "image"; // "image", "video", "document"
                        parameters.Add(new WhatsAppTemplateParameter
                        {
                            Type = mediaType,
                            MediaUrl = mediaUrl
                        });
                    }
                }
                else
                {
                    // Get mappings for text HEADER component (if any)
                    var headerMappings = mappings
                        .Where(m => m.ComponentType == "HEADER")
                        .OrderBy(m => m.VariablePosition)
                        .ToList();

                    foreach (var mapping in headerMappings)
                    {
                        var value = variables.GetValueOrDefault(mapping.VariableType, "-");
                        if (string.IsNullOrWhiteSpace(value))
                            value = "-";
                        parameters.Add(new WhatsAppTemplateParameter
                        {
                            Type = "text",
                            Text = value
                        });
                    }
                }
            }

            if (parameters.Any())
            {
                templateComponent.Parameters = parameters;
            }

            components.Add(templateComponent);
        }

        return components;
    }

    public async Task<bool> SendNotificationWithCostTrackingAsync(Guid companyId, Customer customer, string templateName,
        Dictionary<string, string> variables, string userId, int? moduleId = null, string? referenceId = null, bool? communicationConsent = null)
    {
        try
        {
            // Müşterinin SMS/WhatsApp izni ve telefon numarası kontrolü
            var phoneNumber = customer.MobilePhone ?? customer.Phone;
            customer.SmsPermission = communicationConsent ?? customer.SmsPermission;

            if (!customer.SmsPermission || string.IsNullOrWhiteSpace(phoneNumber))
            {
                _logger.LogInformation("Customer {CustomerId} cannot receive WhatsApp. Permission: {HasPermission}, HasPhone: {HasPhone}",
                    customer.Id, communicationConsent, !string.IsNullOrWhiteSpace(phoneNumber));
                return false;
            }

            // Maliyet hesapla (WhatsApp için varsayılan maliyet)
            decimal cost = 1.00m; // Bu değer ayarlardan alınabilir

            // Şirketin bakiyesini kontrol et
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null || company.CreditBalance < cost)
            {
                _logger.LogWarning("Insufficient credit balance for company {CompanyId}. Required: {Cost}, Available: {Balance}",
                    companyId, cost, company?.CreditBalance ?? 0);
                return false;
            }

            // Bildirimi gönder
            var success = await SendNotificationAsync(companyId, customer, templateName, variables);

            if (success)
            {
                // ModuleUsageService kullanarak harcama kaydı oluştur (sadece geçerli moduleId varsa)
                if (moduleId.HasValue && moduleId.Value > 0)
                {
                    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                    var moduleUsageLogger = loggerFactory.CreateLogger<ModuleUsageService>();
                    var moduleUsageService = new ModuleUsageService(_context, moduleUsageLogger);
                    var result = await moduleUsageService.DeductUsageCostAsync(
                        companyId: companyId,
                        moduleId: moduleId.Value,
                        usageType: "Whatsapp Bildirimi",
                        cost: cost,
                        description: $"WhatsApp bildirimi - {templateName}",
                        userId: userId,
                        referenceId: referenceId,
                        channel: "whatsapp",
                        metadata: new Dictionary<string, object>
                        {
                            ["templateName"] = templateName,
                            ["customerEmail"] = customer.Email,
                            ["customerName"] = customer.FullName,
                            ["sentAt"] = DateTime.Now
                        }
                    );

                    if (!result.Success)
                    {
                        _logger.LogError("Failed to record WhatsApp notification usage: {Message}", result.Message);
                    }
                    else
                    {
                        _logger.LogInformation("WhatsApp notification cost {Cost} deducted from company {CompanyId} balance",
                            cost, companyId);
                    }
                }
                else
                {
                    _logger.LogWarning("WhatsApp notification sent but usage not tracked - moduleId is null or invalid. CompanyId={CompanyId}", companyId);
                }
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp notification with cost tracking for customer {CustomerId}", customer.Id);
            return false;
        }
    }

    public async Task<bool> IsConfiguredAsync(Guid companyId)
    {
        try
        {
            var whatsappIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "WhatsApp" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            if (whatsappIntegration == null)
                return false;

            var settings = whatsappIntegration.Settings;

            return settings != null &&
                   settings.ContainsKey("appId") &&
                   settings.ContainsKey("token") &&
                   !string.IsNullOrEmpty(settings["appId"]?.ToString()) &&
                   !string.IsNullOrEmpty(settings["token"]?.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking WhatsApp configuration for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(Guid companyId)
    {
        try
        {
            var whatsappIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "WhatsApp" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            if (whatsappIntegration == null)
            {
                return ConnectionTestResult.CreateFailure(
                    "WhatsApp entegrasyonu bulunamadı veya yapılandırılmamış.",
                    "WHATSAPP_NOT_CONFIGURED",
                    "WhatsApp"
                );
            }

            var settings = whatsappIntegration.Settings;
            if (settings == null || !settings.Any())
            {
                return ConnectionTestResult.CreateFailure(
                    "WhatsApp ayarları bulunamadı.",
                    "WHATSAPP_SETTINGS_MISSING",
                    "WhatsApp"
                );
            }

            return await TestConnectionWithSettingsAsync(companyId, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing WhatsApp connection for company {CompanyId}", companyId);
            return ConnectionTestResult.CreateError(ex, "WhatsApp");
        }
    }

    public async Task<ConnectionTestResult> TestConnectionWithSettingsAsync(Guid companyId, Dictionary<string, object> settings)
    {
        try
        {
            var whatsappSettings = new WhatsAppConnectionSettings
            {
                AppId = settings.GetValueOrDefault("appId")?.ToString() ?? "",
                Token = settings.GetValueOrDefault("token")?.ToString() ?? "",
            };

            if (string.IsNullOrEmpty(whatsappSettings.AppId) ||
                string.IsNullOrEmpty(whatsappSettings.Token))
            {
                return ConnectionTestResult.CreateFailure(
                    "WhatsApp ayarları eksik. App ID ve Token gereklidir.",
                    "WHATSAPP_SETTINGS_INCOMPLETE",
                    "WhatsApp"
                );
            }

            var startTime = DateTime.Now;
            var result = await _whatsAppService.TestConnectionAsync(whatsappSettings);
            var endTime = DateTime.Now;
            var responseTime = endTime - startTime;

            if (result.Success)
            {
                return ConnectionTestResult.CreateSuccess(
                    result.Message ?? "WhatsApp Business API bağlantısı başarılı!",
                    "WhatsApp",
                    responseTime
                );
            }
            else
            {
                return ConnectionTestResult.CreateFailure(
                    result.Message ?? "WhatsApp bağlantısı başarısız.",
                    result.ErrorCode ?? "WHATSAPP_TEST_FAILED",
                    "WhatsApp"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing WhatsApp connection with settings for company {CompanyId}", companyId);
            return ConnectionTestResult.CreateError(ex, "WhatsApp");
        }
    }

    private async Task<WhatsAppConnectionSettings?> GetWhatsAppSettingsAsync(Guid companyId)
    {
        try
        {
            var whatsappIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "WhatsApp" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            if (whatsappIntegration?.Settings == null)
                return null;

            var settings = whatsappIntegration.Settings;
            return new WhatsAppConnectionSettings
            {
                AppId = settings.GetValueOrDefault("appId")?.ToString() ?? "",
                Token = settings.GetValueOrDefault("token")?.ToString() ?? "",
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WhatsApp settings for company {CompanyId}", companyId);
            return null;
        }
    }
}
