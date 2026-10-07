using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

public class OrderConversion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    [MaxLength(100)]
    public string OrderId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MemberId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "TRY";

    [MaxLength(200)]
    public string? ConversionSource { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation property
    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }
}
