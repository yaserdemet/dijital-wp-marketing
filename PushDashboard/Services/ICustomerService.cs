using PushDashboard.Models;
using PushDashboard.ViewModels;
using PushDashboard.DTOs;

namespace PushDashboard.Services;

public interface ICustomerService
{
    Task<CustomerIndexViewModel> GetCustomersAsync(Guid companyId, int page = 1, int pageSize = 50, string? searchTerm = null, string? status = null, string? source = null, string? dateFilter = null, string? sortBy = null, string? sortDirection = null, DateTime? startDate = null, DateTime? endDate = null);
    Task SyncCustomersAsync(Guid companyId);
    Task<SyncResultDto> SyncCustomersAndBasketsAsync(Guid companyId);
    Task<Customer?> GetCustomerByIdAsync(int id, Guid companyId);
    Task<Customer?> GetCustomerByExternalIdAsync(Guid companyId, int externalId);
    Task<List<SyncLog>> GetSyncLogsAsync(Guid companyId, int limit = 10);
    Task<(int TotalCustomers, int ActiveCustomers, int NewCustomers, int SmsPermittedCustomers, DateTime? LastSync)> GetCustomerStatsAsync(Guid companyId);

    // Yeni metodlar
    Task<int> CreateCustomerAsync(Guid companyId, CreateCustomerViewModel model);
    Task<int> StartBulkImportAsync(Guid companyId, string userId, IFormFile file);
    Task<CustomerImportProgressViewModel?> GetImportProgressAsync(int jobId, Guid companyId);
    Task<List<CustomerImportJobViewModel>> GetImportHistoryAsync(Guid companyId, int limit = 10);
    byte[] GenerateExcelTemplate();
}
