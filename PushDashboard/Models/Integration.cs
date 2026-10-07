using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PushDashboard.Models;

public class Integration
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? DetailedDescription { get; set; }

    public string Type { get; set; } = string.Empty; // "ecommerce" or "communication"

    public string Category { get; set; } = string.Empty; // "E-ticaret Altyapıları" or "İletişim Kanalları"

    public string? IconClass { get; set; }

    public string? IconColor { get; set; }

    public string? BackgroundColor { get; set; }

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsPopular { get; set; } = false;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    // Features as JSON string
    public string? Features { get; set; }

    // Default settings template as JSON string
    public string? DefaultSettingsTemplate { get; set; }

    // Navigation properties
    public virtual ICollection<CompanyIntegration> CompanyIntegrations { get; set; } = new List<CompanyIntegration>();
}

public class CompanyIntegration
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public int IntegrationId { get; set; }

    public virtual Integration Integration { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public bool IsConfigured { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ConfiguredAt { get; set; }

    public DateTime? LastSyncAt { get; set; }

    // Integration-specific settings as JSON
    public string? SettingsJson { get; set; }

    // Sync statistics as JSON
    public string? SyncStatsJson { get; set; }

    // Indicates if the integration was deactivated by the service (not manually by user)
    public bool DeactivatedByService { get; set; } = false;

    // Reason for service-triggered deactivation
    public string? DeactivationReason { get; set; }

    // When the service deactivated this integration
    public DateTime? DeactivatedAt { get; set; }

    public string KvkkLink { get; set; } = string.Empty;

    public string TicariElektronikIletiLink { get; set; } = string.Empty;

    public bool LegalTextsAccepted { get; set; } = false;

    // Helper property to get settings as dictionary
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
        }
    }

    // Helper property to get sync stats as dictionary
    [NotMapped]
    public Dictionary<string, object> SyncStats
    {
        get
        {
            if (string.IsNullOrEmpty(SyncStatsJson))
                return new Dictionary<string, object>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(SyncStatsJson)
                       ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
        set
        {
            SyncStatsJson = JsonSerializer.Serialize(value);
        }
    }

    /// <summary>
    /// Entegrasyon ayarları değiştiğinde true olur.
    /// Senkronizasyon başarılı olduğunda false'a çekilir ve eski veriler silinip yenileri yazılır.
    /// </summary>
    [NotMapped]
    public bool IsNew
    {
        get
        {
            var stats = SyncStats;
            if (stats.TryGetValue("isNew", out var value))
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.GetBoolean();
                }
                return value is bool boolValue && boolValue;
            }
            return false;
        }
        set
        {
            var stats = SyncStats;
            stats["isNew"] = value;
            SyncStats = stats;
        }
    }
}
