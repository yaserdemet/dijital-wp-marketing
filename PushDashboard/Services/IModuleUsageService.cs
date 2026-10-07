using PushDashboard.Models;

namespace PushDashboard.Services;

public interface IModuleUsageService
{
    /// <summary>
    /// Modül kullanımı için company bakiyesinden düşüm yapar ve log kaydı oluşturur
    /// </summary>
    Task<(bool Success, string Message)> DeductUsageCostAsync(
        Guid companyId,
        int moduleId,
        string usageType,
        decimal cost,
        string description,
        string userId,
        string? referenceId = null,
        string? channel = null,
        Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Kullanıcının şirketinin belirli bir modüle sahip olup olmadığını kontrol eder
    /// </summary>
    Task<bool> HasActiveModuleAsync(string userId, int moduleId);

    /// <summary>
    /// Şirketinin belirli bir modüle sahip olup olmadığını kontrol eder
    /// </summary>
    Task<bool> HasActiveModuleAsync(Guid companyId, int moduleId);

    /// <summary>
    /// Kullanıcının şirketinin belirli bir modüle sahip olup olmadığını kontrol eder (modül adı ile)
    /// </summary>
    Task<bool> HasActiveModuleAsync(string userId, string moduleName);

    /// <summary>
    /// Modül ayarlarından maliyet bilgisini alır
    /// </summary>
    Task<decimal> GetModuleUsageCostAsync(Guid companyId, int moduleId, string usageType);

    /// <summary>
    /// Şirketin modül kullanım geçmişini getirir
    /// </summary>
    Task<List<ModuleUsageLog>> GetUsageHistoryAsync(Guid companyId, int? moduleId = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 100, List<string>? excludeUsageTypes = null);

    /// <summary>
    /// Şirketin modül kullanım geçmişini sayfalama ile getirir
    /// </summary>
    Task<List<ModuleUsageLog>> GetUsageHistoryPagedAsync(Guid companyId, int? moduleId = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20, List<string>? excludeUsageTypes = null);

    /// <summary>
    /// Şirketin modül kullanım geçmişi toplam kayıt sayısını getirir
    /// </summary>
    Task<int> GetUsageHistoryCountAsync(Guid companyId, int? moduleId = null, DateTime? startDate = null, DateTime? endDate = null, List<string>? excludeUsageTypes = null);

    /// <summary>
    /// Modül kullanımı için log kaydı oluşturur ve maliyet düşer
    /// </summary>
    Task<(bool Success, string Message)> LogUsageAndDeductCostAsync(
        Guid companyId,
        int moduleId,
        string usageType,
        decimal cost,
        string description,
        string userId,
        string? referenceId = null,
        string? channel = null);

    /// <summary>
    /// Şirketin modül kullanım istatistiklerini getirir
    /// </summary>
    Task<ModuleUsageStats> GetUsageStatsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null, List<string>? excludeUsageTypes = null);
}

public class ModuleUsageStats
{
    public decimal TotalSpent { get; set; }
    public int TotalUsages { get; set; }
    public Dictionary<string, decimal> SpentByModule { get; set; } = new();
    public Dictionary<string, decimal> SpentByUsageType { get; set; } = new();
    public Dictionary<string, int> UsagesByModule { get; set; } = new();
    public Dictionary<string, int> UsagesByType { get; set; } = new();
}
