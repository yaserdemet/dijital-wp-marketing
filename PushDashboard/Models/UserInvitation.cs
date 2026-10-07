using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

public class UserInvitation
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public string InvitationToken { get; set; } = string.Empty;

    public DateTime ExpirationDate { get; set; }

    public bool IsUsed { get; set; } = false;

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public virtual ApplicationUser CreatedByUser { get; set; } = null!;

    // Helper properties
    [NotMapped]
    public bool IsExpired => DateTime.Now > ExpirationDate;

    [NotMapped]
    public bool IsValid => !IsUsed && !IsExpired;

    [NotMapped]
    public string StatusText
    {
        get
        {
            if (IsUsed) return "Kullanıldı";
            if (IsExpired) return "Süresi Doldu";
            return "Beklemede";
        }
    }

    [NotMapped]
    public string StatusBadgeClass
    {
        get
        {
            if (IsUsed) return "bg-green-100 text-green-800";
            if (IsExpired) return "bg-red-100 text-red-800";
            return "bg-yellow-100 text-yellow-800";
        }
    }
}
