using PushDashboard.Models;

namespace PushDashboard.Services.Integrations.Ticimax;

public interface ITicimaxBasketService
{
    Task<(bool Success, string Message)> SyncBasketsAsync(Guid companyId, Dictionary<string, object> settings);
    Task<Basket[]> FetchBasketsFromApiAsync(Dictionary<string, object> settings, int pageNumber);
}
