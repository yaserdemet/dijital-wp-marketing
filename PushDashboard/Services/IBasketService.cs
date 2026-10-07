using PushDashboard.ViewModels;

namespace PushDashboard.Services;

public interface IBasketService
{
    Task<BasketIndexViewModel> GetBasketsAsync(Guid companyId, int page, int pageSize, BasketAdvancedFilter filter);
    Task<BasketIndexViewModel.BasketViewModel?> GetBasketDetailsAsync(string guidBasketId, Guid companyId);
    Task<BasketIndexViewModel.BasketItemsPagedViewModel> GetBasketItemsPagedAsync(string guidBasketId, Guid companyId, int page = 1, int pageSize = 10);
    Task<List<BasketIndexViewModel.CurrencyTotalViewModel>> GetCurrencyTotalsAsync(Guid companyId);
    Task<List<BasketIndexViewModel.BasketViewModel>> GetBasketsByCustomerAsync(int customerId, Guid companyId);
    Task SyncBasketsAsync(Guid companyId);
    Task<List<BasketIndexViewModel.BasketSyncLogViewModel>> GetRecentSyncLogsAsync(Guid companyId, int count = 10);
    
    // Abandoned time management
    Task<int> GetAbandonedTimeAsync(Guid companyId);
    Task UpdateAbandonedTimeAsync(Guid companyId, int hours);
}
