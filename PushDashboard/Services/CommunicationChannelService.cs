using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Services.WhatsApp;
using System.Text.Json;

namespace PushDashboard.Services;

public interface ICommunicationChannelService
{
    Task<List<CommunicationChannel>> GetAvailableChannelsAsync(Guid companyId);
    Task<bool> IsChannelActiveAsync(Guid companyId, string channelName);
    Task<Dictionary<string, bool>> GetChannelStatusAsync(Guid companyId);
}

public class CommunicationChannel
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string ColorClass { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CommunicationChannelService : ICommunicationChannelService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<CommunicationChannelService> _logger;

    public CommunicationChannelService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<CommunicationChannelService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<List<CommunicationChannel>> GetAvailableChannelsAsync(Guid companyId)
    {
        var channels = new List<CommunicationChannel>();

        // Email kanalını kontrol et
        var emailActive = await _emailService.IsConfiguredAsync();
        channels.Add(new CommunicationChannel
        {
            Name = "Email",
            DisplayName = "E-posta",
            IconClass = "mail",
            ColorClass = "text-blue-600",
            IsActive = emailActive,
            Description = emailActive ? "E-posta gönderimi aktif" : "E-posta ayarları yapılmamış"
        });

        // SMS kanalını kontrol et
        var smsActive = await IsSmsConfiguredAsync(companyId);
        channels.Add(new CommunicationChannel
        {
            Name = "SMS",
            DisplayName = "SMS",
            IconClass = "smartphone",
            ColorClass = "text-yellow-600",
            IsActive = smsActive,
            Description = smsActive ? "SMS gönderimi aktif" : "SMS ayarları yapılmamış"
        });

        // WhatsApp kanalını kontrol et
        var whatsAppActive = await IsWhatsAppConfiguredAsync(companyId);
        channels.Add(new CommunicationChannel
        {
            Name = "WhatsApp",
            DisplayName = "WhatsApp",
            IconClass = "message-circle",
            ColorClass = "text-green-600",
            IsActive = whatsAppActive,
            Description = whatsAppActive ? "WhatsApp entegrasyonu aktif" : "WhatsApp entegrasyonu yapılmamış"
        });

        return channels;
    }

    public async Task<bool> IsChannelActiveAsync(Guid companyId, string channelName)
    {
        return channelName switch
        {
            "Email" => await _emailService.IsConfiguredAsync(),
            "SMS" => await IsSmsConfiguredAsync(companyId),
            "WhatsApp" => await IsWhatsAppConfiguredAsync(companyId),
            "Telegram" => await IsTelegramConfiguredAsync(companyId),
            _ => false
        };
    }

    public async Task<Dictionary<string, bool>> GetChannelStatusAsync(Guid companyId)
    {
        var status = new Dictionary<string, bool>();

        status["Email"] = await _emailService.IsConfiguredAsync();
        status["SMS"] = await IsSmsConfiguredAsync(companyId);
        status["WhatsApp"] = await IsWhatsAppConfiguredAsync(companyId);
        status["Telegram"] = await IsTelegramConfiguredAsync(companyId);

        return status;
    }

    private async Task<bool> IsWhatsAppConfiguredAsync(Guid companyId)
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

            // WhatsApp için gerekli ayarları kontrol et
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

    private async Task<bool> IsSmsConfiguredAsync(Guid companyId)
    {
        try
        {
            var integration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "SMS" &&
                                         ci.IsActive);

            if (integration?.SettingsJson == null || string.IsNullOrWhiteSpace(integration.SettingsJson))
                return false;

            // SettingsJson string alanını güvenli şekilde deserialize et
            Dictionary<string, JsonElement>? settings;
            try
            {
                settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(integration.SettingsJson);
            }
            catch (JsonException)
            {
                // JSON parse hatası durumunda false döndür
                return false;
            }

            if (settings == null)
                return false;

            // SMS (NetGSM) için gerekli ayarları kontrol et
            return settings.ContainsKey("username") &&
                   settings.ContainsKey("password") &&
                   settings.ContainsKey("senderName") &&
                   settings["username"].ValueKind != JsonValueKind.Null &&
                   settings["password"].ValueKind != JsonValueKind.Null &&
                   settings["senderName"].ValueKind != JsonValueKind.Null &&
                   !string.IsNullOrEmpty(settings["username"].GetString()) &&
                   !string.IsNullOrEmpty(settings["password"].GetString()) &&
                   !string.IsNullOrEmpty(settings["senderName"].GetString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SMS configuration for company {CompanyId}", companyId);
            return false;
        }
    }

    private async Task<bool> IsTelegramConfiguredAsync(Guid companyId)
    {
        try
        {
            var integration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "Telegram" &&
                                         ci.IsActive);

            if (integration?.SettingsJson == null || string.IsNullOrWhiteSpace(integration.SettingsJson))
                return false;

            // SettingsJson string alanını güvenli şekilde deserialize et
            Dictionary<string, JsonElement>? settings;
            try
            {
                settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(integration.SettingsJson);
            }
            catch (JsonException)
            {
                // JSON parse hatası durumunda false döndür
                return false;
            }

            if (settings == null)
                return false;

            // Telegram için gerekli ayarları kontrol et
            return settings.ContainsKey("botToken") &&
                   settings["botToken"].ValueKind != JsonValueKind.Null &&
                   !string.IsNullOrEmpty(settings["botToken"].GetString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Telegram configuration for company {CompanyId}", companyId);
            return false;
        }
    }
}
