using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

public class ApplicationUser : IdentityUser
{
    public ApplicationUser()
    {
        // Varsayılan olarak giriş yapamaz - onay gerekiyor
        LockoutEnabled = false;
    }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    // Foreign key for Company
    public Guid? CompanyId { get; set; }

    // Navigation property for Company
    public virtual Company? Company { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? LastLoginAt { get; set; }

    // Two Factor Authentication
    public string? TwoFactorSecretKey { get; set; }

    // Credit Balance
    public decimal CreditBalance { get; set; } = 0.00m;

    // Referral Code (sadece 6 haneli sayı, veritabanında "PRef-" prefix'i olmadan)
    public string? ReferralCode { get; set; }

    // SuperAdmin - can switch between companies
    public bool IsSuperAdmin { get; set; } = false;

    // Referral Count (bu kullanıcının referans kodu ile kaç kişi kayıt oldu)
    public int ReferralCount { get; set; } = 0;

    // Referred By Referral Code (hangi referans kodu ile kayıt oldu - sadece 6 haneli sayı, "PRef-" prefix'i olmadan)
    public string? ReferredByReferralCode { get; set; }

    // Helper property to get full name
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    // Helper property for display (UI'da "PRef-" prefix'i ile gösterim)
    [NotMapped]
    public string ReferralCodeDisplay => !string.IsNullOrEmpty(ReferralCode) ? $"PRef-{ReferralCode}" : string.Empty;

    // Navigation property for notification preferences
    public virtual NotificationPreferences? NotificationPreferences { get; set; }

    // Navigation property for user sessions
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
