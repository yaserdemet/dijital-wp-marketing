namespace PushDashboard.Services.Modules.Birthday;

public interface IBirthdayModuleService
{
    Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> SendBirthdayNotificationsAsync(Guid companyId, string userId);
    Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> SendBirthdayNotificationsForAllCompaniesAsync();
}
