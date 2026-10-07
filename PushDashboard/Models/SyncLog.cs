using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

public class SyncLog
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public string SyncType { get; set; } = string.Empty; // "Customer", "Basket", "Order" vb.

    public DateTime SyncStartTime { get; set; }

    public DateTime? SyncEndTime { get; set; }

    public int TotalRecordsProcessed { get; set; } = 0;

    public int NewRecordsAdded { get; set; } = 0;

    public int RecordsUpdated { get; set; } = 0;

    public bool IsSuccessful { get; set; } = false;

    public string? ErrorMessage { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Helper properties
    [NotMapped]
    public string StatusText => IsSuccessful ? "Başarılı" : "Başarısız";

    [NotMapped]
    public string StatusBadgeClass => IsSuccessful ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";

    [NotMapped]
    public string FormattedSyncStartTime => SyncStartTime.ToString("dd.MM.yyyy HH:mm:ss");

    [NotMapped]
    public string FormattedSyncEndTime => SyncEndTime?.ToString("dd.MM.yyyy HH:mm:ss") ?? "Devam ediyor";

    [NotMapped]
    public string Duration
    {
        get
        {
            if (SyncEndTime.HasValue)
            {
                var duration = SyncEndTime.Value - SyncStartTime;
                return $"{duration.TotalSeconds:F1} saniye";
            }
            return "Devam ediyor";
        }
    }
}
