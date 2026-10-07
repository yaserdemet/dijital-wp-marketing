using PushDashboard.Models;

namespace PushDashboard.Services.Notifications;

public interface INotificationChannelService
{
    string ChannelType { get; }
    Task<bool> SendNotificationAsync(Guid companyId, Customer customer, string templateName, Dictionary<string, string> variables);
    Task<bool> SendNotificationWithCostTrackingAsync(Guid companyId, Customer customer, string templateName, Dictionary<string, string> variables, string userId, int? moduleId = null, string? referenceId = null, bool? communicationConsent = null);
    Task<bool> IsConfiguredAsync(Guid companyId);
    Task<ConnectionTestResult> TestConnectionAsync(Guid companyId);
    Task<ConnectionTestResult> TestConnectionWithSettingsAsync(Guid companyId, Dictionary<string, object> settings);
}
