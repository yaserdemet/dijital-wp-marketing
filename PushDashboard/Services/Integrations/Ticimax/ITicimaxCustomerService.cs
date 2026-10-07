using PushDashboard.Models;

namespace PushDashboard.Services.Integrations.Ticimax;

public interface ITicimaxCustomerService
{
    Task<(bool Success, string Message)> SyncCustomersAsync(Guid companyId, Dictionary<string, object> settings);
    Task<Customer[]> FetchCustomersFromApiAsync(Dictionary<string, object> settings, int pageNumber, int pageSize);
}
