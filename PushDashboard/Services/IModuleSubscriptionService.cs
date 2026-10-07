namespace PushDashboard.Services;

/// <summary>
/// Modül abonelik yenileme sonucu
/// </summary>
public class RenewalResult
{
    public Guid CompanyId { get; set; }
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal? AmountCharged { get; set; }
    public DateTime? NewExpiryDate { get; set; }
}

/// <summary>
/// Süresi dolmak üzere olan abonelik bilgisi
/// </summary>
public class ExpiringSubscriptionDto
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int DaysUntilExpiry { get; set; }
    public bool AutoRenewal { get; set; }
    public decimal RenewalPrice { get; set; }
}

/// <summary>
/// Modül abonelik yönetimi servisi
/// </summary>
public interface IModuleSubscriptionService
{
    /// <summary>
    /// Süresi dolan abonelikleri kontrol edip yeniler
    /// </summary>
    Task<List<RenewalResult>> ProcessExpiredSubscriptionsAsync();

    /// <summary>
    /// Belirli bir şirketin modül aboneliğini yeniler
    /// </summary>
    Task<RenewalResult> RenewSubscriptionAsync(Guid companyId, int moduleId);

    /// <summary>
    /// Yakında süresi dolacak abonelikleri listeler
    /// </summary>
    Task<List<ExpiringSubscriptionDto>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry = 3);
}
