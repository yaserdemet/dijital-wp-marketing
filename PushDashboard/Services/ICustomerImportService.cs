using PushDashboard.Models;
using PushDashboard.ViewModels;

namespace PushDashboard.Services;

public interface ICustomerImportService
{
    /// <summary>
    /// Excel dosyasını yükler ve import job'ı oluşturur
    /// </summary>
    Task<int> CreateImportJobAsync(Guid companyId, string userId, IFormFile file);

    /// <summary>
    /// Import job'ının durumunu getirir
    /// </summary>
    Task<CustomerImportProgressViewModel?> GetImportProgressAsync(int jobId, Guid companyId);

    /// <summary>
    /// Şirketin import geçmişini getirir
    /// </summary>
    Task<List<CustomerImportJobViewModel>> GetImportHistoryAsync(Guid companyId, int limit = 10);

    /// <summary>
    /// Excel şablonunu oluşturur ve indirir
    /// </summary>
    byte[] GenerateExcelTemplate();

    /// <summary>
    /// Yeni müşteri oluşturur
    /// </summary>
    Task<int> CreateCustomerAsync(Guid companyId, CreateCustomerViewModel model);

    /// <summary>
    /// Excel dosyasını işler (background service tarafından çağrılır)
    /// </summary>
    Task ProcessImportJobAsync(int jobId);
}
