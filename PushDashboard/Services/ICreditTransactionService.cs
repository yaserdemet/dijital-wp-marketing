using PushDashboard.Models;

namespace PushDashboard.Services;

public interface ICreditTransactionService
{
    /// <summary>
    /// Yeni ödeme işlemi kaydı oluşturur (Pending durumunda)
    /// </summary>
    Task<CreditTransaction> CreatePaymentTransactionAsync(Guid companyId, decimal amount, string userId, string paytrOrderId);

    /// <summary>
    /// Başarılı ödeme sonrası işlemi tamamlar ve bakiyeyi günceller
    /// </summary>
    Task<bool> CompleteTransactionAsync(string paytrOrderId, string? paytrTransactionId, string? cardLastFour);

    /// <summary>
    /// Başarısız ödeme sonrası işlemi Failed olarak işaretler
    /// </summary>
    Task<bool> FailTransactionAsync(string paytrOrderId, string? errorMessage);

    /// <summary>
    /// Şirketin kredi işlem geçmişini sayfalanmış olarak getirir
    /// </summary>
    Task<List<CreditTransaction>> GetTransactionHistoryAsync(Guid companyId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Şirketin toplam işlem sayısını getirir
    /// </summary>
    Task<int> GetTransactionCountAsync(Guid companyId);
}
