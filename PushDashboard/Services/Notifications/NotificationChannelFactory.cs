using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;

namespace PushDashboard.Services.Notifications;

public interface INotificationChannelFactory
{
    Task<List<INotificationChannelService>> GetActiveChannelsAsync(Guid companyId);
    Task<INotificationChannelService?> GetChannelAsync(Guid companyId, string channelType);
}

public class NotificationChannelFactory : INotificationChannelFactory
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationChannelFactory> _logger;

    public NotificationChannelFactory(
        ApplicationDbContext context,
        IServiceProvider serviceProvider,
        ILogger<NotificationChannelFactory> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<List<INotificationChannelService>> GetActiveChannelsAsync(Guid companyId)
    {
        var activeChannels = new List<INotificationChannelService>();

        try
        {
            // Şirketin aktif iletişim kanallarını al
            var activeIntegrations = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .Where(ci => ci.CompanyId == companyId &&
                           ci.IsActive &&
                           ci.IsConfigured &&
                           ci.Integration.Type == "communication")
                .ToListAsync();

            foreach (var integration in activeIntegrations)
            {
                var channelService = GetChannelServiceByIntegrationName(integration.Integration.Name);
                if (channelService != null)
                {
                    var isConfigured = await channelService.IsConfiguredAsync(companyId);
                    if (isConfigured)
                    {
                        activeChannels.Add(channelService);
                    }
                }
            }

            _logger.LogInformation("Found {Count} active notification channels for company {CompanyId}",
                activeChannels.Count, companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active channels for company {CompanyId}", companyId);
        }

        return activeChannels;
    }

    public async Task<INotificationChannelService?> GetChannelAsync(Guid companyId, string channelType)
    {
        try
        {
            INotificationChannelService channelService = null;
            switch (channelType.ToLower())
            {
                case "email":
                    channelService = _serviceProvider.GetService<EmailNotificationChannelService>();
                    break;
                case "whatsapp":
                    channelService = _serviceProvider.GetService<WhatsAppNotificationChannelService>();
                    break;
                case "sms":
                    channelService = _serviceProvider.GetService<SmsNotificationChannelService>();
                    break;
                default:
                    break;
            }

            if (channelService != null)
            {
                var isConfigured = await channelService.IsConfiguredAsync(companyId);
                if (!isConfigured)
                {
                    _logger.LogWarning("Channel {ChannelType} is not configured for company {CompanyId}",
                        channelType, companyId);
                    return null;
                }
            }

            return channelService;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting channel {ChannelType} for company {CompanyId}",
                channelType, companyId);
            return null;
        }
    }

    private INotificationChannelService? GetChannelServiceByIntegrationName(string integrationName)
    {
        return integrationName switch
        {
            "Email SMTP" => _serviceProvider.GetService<EmailNotificationChannelService>(),
            "WhatsApp" => _serviceProvider.GetService<WhatsAppNotificationChannelService>(),
            "SMS" => _serviceProvider.GetService<SmsNotificationChannelService>(),
            // Gelecekte diğer entegrasyonlar buraya eklenecek
            // "Telegram" => _serviceProvider.GetService<TelegramNotificationChannelService>(),
            _ => null
        };
    }
}
