using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

public class ModulePackage
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public virtual Module Module { get; set; } = null!;

    [Required]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    public decimal QuotaAmount { get; set; }

    /// <summary>Aylık abonelik fiyatı</summary>
    [DataType(DataType.Currency)]
    public decimal MonthlyPrice { get; set; }

    /// <summary>Yıllık abonelik fiyatı (indirimli)</summary>
    [DataType(DataType.Currency)]
    public decimal YearlyPrice { get; set; }

    /// <summary>Yıllık tasarruf yüzdesi (hesaplanmış)</summary>
    [NotMapped]
    public int YearlyDiscountPercent =>
        MonthlyPrice > 0
            ? (int)(100 - (YearlyPrice / (MonthlyPrice * 12)) * 100)
            : 0;

    public string? HtmlContent { get; set; }

    public bool IsActive { get; set; } = true;
}
