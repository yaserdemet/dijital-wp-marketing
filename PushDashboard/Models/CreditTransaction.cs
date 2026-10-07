using PushDashboard.Models.Enums;

namespace PushDashboard.Models;

/// <summary>
/// Kredi bakiye hareketlerini kaydeder (ödeme, iade, manuel düzeltme)
/// </summary>
public class CreditTransaction
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    public CreditTransactionType TransactionType { get; set; }

    /// <summary>
    /// İşlem tutarı (pozitif = bakiyeye ekleme, negatif = bakiyeden düşme)
    /// </summary>
    public decimal Amount { get; set; }

    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// PayTR sipariş numarası (merchant_oid)
    /// </summary>
    public string? PaytrOrderId { get; set; }

    /// <summary>
    /// PayTR'den dönen işlem ID'si
    /// </summary>
    public string? PaytrTransactionId { get; set; }

    /// <summary>
    /// Kullanılan kartın son 4 hanesi
    /// </summary>
    public string? CardLastFour { get; set; }

    /// <summary>
    /// İşlem durumu: Pending, Success, Failed
    /// </summary>
    public string Status { get; set; } = "Pending";

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedByUserId { get; set; } = string.Empty;
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;
}
