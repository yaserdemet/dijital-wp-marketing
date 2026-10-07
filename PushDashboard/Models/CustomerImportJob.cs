using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

public class CustomerImportJob
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string Status { get; set; } = "Bekliyor"; // Bekliyor, İşleniyor, Tamamlandı, Hata

    public int TotalRows { get; set; } = 0;

    public int ProcessedRows { get; set; } = 0;

    public int SuccessCount { get; set; } = 0;

    public int ErrorCount { get; set; } = 0;

    public string? ErrorDetails { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public virtual ApplicationUser CreatedByUser { get; set; } = null!;

    // Helper properties
    [NotMapped]
    public string StatusText => Status switch
    {
        "Bekliyor" => "Bekliyor",
        "İşleniyor" => "İşleniyor",
        "Tamamlandı" => "Tamamlandı",
        "Hata" => "Hata",
        _ => "Bilinmeyen"
    };

    [NotMapped]
    public string StatusBadgeClass => Status switch
    {
        "Bekliyor" => "bg-yellow-100 text-yellow-800",
        "İşleniyor" => "bg-blue-100 text-blue-800",
        "Tamamlandı" => "bg-green-100 text-green-800",
        "Hata" => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };

    [NotMapped]
    public int ProgressPercentage => TotalRows > 0 ? (int)((double)ProcessedRows / TotalRows * 100) : 0;

    [NotMapped]
    public bool IsCompleted => Status == "Tamamlandı" || Status == "Hata";

    [NotMapped]
    public bool IsProcessing => Status == "İşleniyor";

    [NotMapped]
    public bool HasErrors => ErrorCount > 0;
}
