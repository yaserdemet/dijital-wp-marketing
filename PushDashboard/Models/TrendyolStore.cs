using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Trendyol mağaza bilgilerini tutar
/// </summary>
public class TrendyolStore
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid CompanyId { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string StoreUrl { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ExternalStoreId { get; set; }

    [StringLength(200)]
    public string? StoreName { get; set; }

    [Required]
    [StringLength(50)]
    public string SyncStatus { get; set; } = "Pending"; // Pending, Syncing, Completed, Failed

    public DateTime? LastSyncAt { get; set; }

    public int? ProductCount { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<TrendyolProduct> Products { get; set; } = new List<TrendyolProduct>();
    public virtual ICollection<CommentTransferJob> TransferJobs { get; set; } = new List<CommentTransferJob>();

    // Helper properties
    [NotMapped]
    public string StatusBadgeClass => SyncStatus switch
    {
        "Pending" => "bg-gray-100 text-gray-800",
        "Syncing" => "bg-blue-100 text-blue-800",
        "Completed" => "bg-green-100 text-green-800",
        "Failed" => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };

    [NotMapped]
    public string StatusIcon => SyncStatus switch
    {
        "Pending" => "clock",
        "Syncing" => "refresh-cw",
        "Completed" => "check-circle",
        "Failed" => "x-circle",
        _ => "clock"
    };
}
