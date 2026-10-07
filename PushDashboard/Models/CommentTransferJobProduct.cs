using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Transfer işine dahil edilen ürünleri tutar
/// </summary>
public class CommentTransferJobProduct
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int JobId { get; set; }

    [ForeignKey("JobId")]
    public virtual CommentTransferJob Job { get; set; } = null!;

    public int? ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual TrendyolProduct? Product { get; set; }

    [Required]
    [StringLength(1000)]
    public string ProductUrl { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed

    public int? CommentsCount { get; set; }

    [StringLength(1000)]
    public string? CommentsFileUrl { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ProcessedAt { get; set; }

    // Helper properties
    [NotMapped]
    public string StatusBadgeClass => Status switch
    {
        "Pending" => "bg-gray-100 text-gray-800",
        "Processing" => "bg-blue-100 text-blue-800",
        "Completed" => "bg-green-100 text-green-800",
        "Failed" => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };
}
