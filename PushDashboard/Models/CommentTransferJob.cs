using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Yorum transfer işlerini takip eder
/// </summary>
public class CommentTransferJob
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid CompanyId { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    public int? StoreId { get; set; }

    [ForeignKey("StoreId")]
    public virtual TrendyolStore? Store { get; set; }

    [Required]
    [StringLength(100)]
    public string JobId { get; set; } = string.Empty; // External API job ID

    [StringLength(100)]
    public string? ExternalBatchId { get; set; }

    [Required]
    [StringLength(50)]
    public string JobType { get; set; } = "Comments"; // Products, Comments

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed

    public int? ProgressPercent { get; set; }

    [StringLength(200)]
    public string? CurrentStep { get; set; }

    public int? TotalProducts { get; set; }

    public int? ProcessedProducts { get; set; }

    public int? TotalComments { get; set; }

    [StringLength(1000)]
    public string? ResultFileUrl { get; set; }

    [StringLength(1000)]
    public string? LogsFileUrl { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Auto-Sync Properties
    public bool IsAutoTransferEnabled { get; set; }

    public DateTime? LastAutoTransferDate { get; set; }

    [Column(TypeName = "jsonb")]
    public string? TransferSettings { get; set; }

    // Navigation properties
    public virtual ICollection<CommentTransferJobProduct> JobProducts { get; set; } = new List<CommentTransferJobProduct>();

    // Helper properties
    [NotMapped]
    public string StatusBadgeClass => Status switch
    {
        "Pending" => "bg-gray-100 text-gray-800",
        "Running" => "bg-blue-100 text-blue-800",
        "Completed" => "bg-green-100 text-green-800",
        "Failed" => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };

    [NotMapped]
    public string StatusIcon => Status switch
    {
        "Pending" => "clock",
        "Running" => "refresh-cw",
        "Completed" => "check-circle",
        "Failed" => "x-circle",
        _ => "clock"
    };

    [NotMapped]
    public string JobTypeDisplay => JobType switch
    {
        "Products" => "Ürün Senkronizasyonu",
        "Comments" => "Yorum Transfer",
        _ => JobType
    };
}
