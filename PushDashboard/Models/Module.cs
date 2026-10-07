using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PushDashboard.Models;

public class Module
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? DetailedDescription { get; set; }

    public decimal Price { get; set; }

    /// <summary>
    /// Aylık abonelik fiyatı
    /// </summary>
    public decimal MonthlyPrice { get; set; }

    /// <summary>
    /// Yıllık abonelik fiyatı (indirimli)
    /// </summary>
    public decimal YearlyPrice { get; set; }

    public string? IconClass { get; set; }

    public string? IconColor { get; set; }

    public string? BackgroundColor { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsNew { get; set; } = false;

    public bool IsFeatured { get; set; } = false;

    public int CategoryId { get; set; }

    public virtual ModuleCategory Category { get; set; } = null!;

    public bool HasQuotaSystem { get; set; } = false;
    public bool HasPackages { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    // Features as JSON string
    public string? Features { get; set; }

    // Default settings as JSON string
    public string? DefaultSettings { get; set; }
    // Navigation properties
    public virtual ICollection<CompanyModule> CompanyModules { get; set; } = new List<CompanyModule>();
}

public class ModuleCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? IconClass { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
}

/// <summary>
/// Company-based module ownership and management
/// </summary>
public class CompanyModule
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public int ModuleId { get; set; }

    public virtual Module Module { get; set; } = null!;

    public DateTime PurchasedAt { get; set; } = DateTime.Now;

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsPassive { get; set; }
    public bool IsOwned { get; set; }

    public decimal PaidAmount { get; set; }

    public string? TransactionId { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Abonelik tipi (Aylık/Yıllık)
    /// </summary>
    public SubscriptionType SubscriptionType { get; set; } = SubscriptionType.Monthly;

    /// <summary>
    /// Otomatik yenileme aktif mi?
    /// </summary>
    public bool AutoRenewal { get; set; } = true;

    /// <summary>
    /// Son yenileme tarihi
    /// </summary>
    public DateTime? LastRenewalDate { get; set; }

    /// <summary>
    /// Toplam yenileme sayısı
    /// </summary>
    public int RenewalCount { get; set; } = 0;


    public string PurchasedByUserId { get; set; } = string.Empty;

    public virtual ApplicationUser PurchasedByUser { get; set; } = null!;

    // Navigation property for settings
    public virtual CompanyModuleSettings? Settings { get; set; }
}

/// <summary>
/// Company module settings - shared across all company users
/// </summary>
public class CompanyModuleSettings
{
    public int Id { get; set; }

    public int CompanyModuleId { get; set; }

    public virtual CompanyModule CompanyModule { get; set; } = null!;

    // Settings stored as JSON
    public string? SettingsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string UpdatedByUserId { get; set; } = string.Empty;

    public virtual ApplicationUser UpdatedByUser { get; set; } = null!;

    // Computed property for settings
    [NotMapped]
    public Dictionary<string, object> Settings
    {
        get
        {
            if (string.IsNullOrEmpty(SettingsJson))
                return new Dictionary<string, object>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(SettingsJson)
                       ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
        set
        {
            SettingsJson = JsonSerializer.Serialize(value);
            UpdatedAt = DateTime.Now;
        }
    }
}
