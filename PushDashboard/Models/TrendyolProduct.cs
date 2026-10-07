using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Trendyol mağaza ürünlerini tutar
/// </summary>
public class TrendyolProduct
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StoreId { get; set; }

    [ForeignKey("StoreId")]
    public virtual TrendyolStore Store { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Href { get; set; } = string.Empty;

    public bool IsSelected { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<CommentTransferJobProduct> JobProducts { get; set; } = new List<CommentTransferJobProduct>();

    // Helper properties
    [NotMapped]
    public string FullUrl => Href.StartsWith("http") ? Href : $"https://www.trendyol.com{Href}";

    [NotMapped]
    public string ShortTitle => Title.Length > 50 ? Title.Substring(0, 50) + "..." : Title;
}
