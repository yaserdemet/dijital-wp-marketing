namespace PushDashboard.Models;

/// <summary>
/// Modül kullanım maliyetlerini ve harcama takibini yönetir
/// </summary>
public class ModuleUsageLog
{
    public int Id { get; set; }

    public Guid? CompanyId { get; set; }

    public virtual Company? Company { get; set; }

    public int ModuleId { get; set; }

    public virtual Module Module { get; set; } = null!;

    public string UsageType { get; set; } = string.Empty; // "email", "sms", "whatsapp", etc.

    public string Description { get; set; } = string.Empty; // "Birthday notification sent to customer@example.com"

    public decimal Cost { get; set; } // Bu kullanım için düşülen tutar

    public decimal BalanceBefore { get; set; } // İşlem öncesi bakiye

    public decimal BalanceAfter { get; set; } // İşlem sonrası bakiye

    public string UserId { get; set; } = string.Empty; // İşlemi tetikleyen kullanıcı

    public virtual ApplicationUser User { get; set; } = null!;

    public string? ReferenceId { get; set; } // Müşteri ID, bildirim ID, vb.

    public string? Channel { get; set; } // "email", "sms", "whatsapp"

    public bool IsSuccessful { get; set; } = true; // İşlem başarılı mı?

    public string? ErrorMessage { get; set; } // Hata mesajı (varsa)

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // JSON metadata for additional information
    public string? Metadata { get; set; }
}
