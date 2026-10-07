using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PushDashboard.Models;

/// <summary>
/// Toplu mesaj gönderim işlemi
/// </summary>
public class BulkMessage
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public virtual ApplicationUser User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Hedef müşteri filtreleri (JSON)
    public string? CustomerFiltersJson { get; set; }

    // Aktif kanallar ve şablonları (JSON)
    public string? ChannelSettingsJson { get; set; }

    // Gönderim durumu
    public string Status { get; set; } = "Hazırlanıyor"; // Hazırlanıyor, Sırada, Gönderiliyor, Tamamlandı, Hata

    // İstatistikler
    public int TotalRecipients { get; set; } = 0;
    public int ProcessedRecipients { get; set; } = 0;
    public int SuccessfulSends { get; set; } = 0;
    public int FailedSends { get; set; } = 0;

    // Maliyet bilgileri
    public decimal EstimatedCost { get; set; } = 0.00m;

    public decimal ActualCost { get; set; } = 0.00m;

    // Zaman bilgileri
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Hata mesajı
    public string? ErrorMessage { get; set; }

    // Progress tracking
    public int CurrentBatch { get; set; } = 0;
    public int TotalBatches { get; set; } = 0;

    // Navigation properties
    public virtual ICollection<BulkMessageRecipient> Recipients { get; set; } = new List<BulkMessageRecipient>();

    // Helper properties
    [NotMapped]
    public Dictionary<string, object> CustomerFilters
    {
        get
        {
            if (string.IsNullOrEmpty(CustomerFiltersJson))
                return new Dictionary<string, object>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(CustomerFiltersJson)
                       ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
        set
        {
            CustomerFiltersJson = JsonSerializer.Serialize(value);
        }
    }

    [NotMapped]
    public Dictionary<string, object> ChannelSettings
    {
        get
        {
            if (string.IsNullOrEmpty(ChannelSettingsJson))
                return new Dictionary<string, object>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(ChannelSettingsJson)
                       ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
        set
        {
            ChannelSettingsJson = JsonSerializer.Serialize(value);
        }
    }

    [NotMapped]
    public string StatusBadgeClass => Status switch
    {
        "Hazırlanıyor" => "bg-yellow-100 text-yellow-800",
        "Sırada" => "bg-blue-100 text-blue-800",
        "Gönderiliyor" => "bg-purple-100 text-purple-800",
        "Tamamlandı" => "bg-green-100 text-green-800",
        "Hata" => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };

    [NotMapped]
    public double ProgressPercentage
    {
        get
        {
            if (TotalRecipients == 0) return 0;
            return Math.Round((double)ProcessedRecipients / TotalRecipients * 100, 1);
        }
    }

    [NotMapped]
    public double SuccessRate
    {
        get
        {
            if (ProcessedRecipients == 0) return 0;
            return Math.Round((double)SuccessfulSends / ProcessedRecipients * 100, 1);
        }
    }

    [NotMapped]
    public string FormattedEstimatedCost => $"₺{EstimatedCost:N2}";

    [NotMapped]
    public string FormattedActualCost => $"₺{ActualCost:N2}";

    [NotMapped]
    public string FormattedCreatedAt => CreatedAt.ToString("dd.MM.yyyy HH:mm");

    [NotMapped]
    public string FormattedDuration
    {
        get
        {
            if (StartedAt == null) return "Henüz başlamadı";
            if (CompletedAt == null) return $"Devam ediyor ({DateTime.Now.Subtract(StartedAt.Value).TotalMinutes:F0} dk)";

            var duration = CompletedAt.Value.Subtract(StartedAt.Value);
            if (duration.TotalMinutes < 1)
                return $"{duration.Seconds} saniye";
            else if (duration.TotalHours < 1)
                return $"{duration.Minutes} dakika {duration.Seconds} saniye";
            else
                return $"{duration.Hours} saat {duration.Minutes} dakika";
        }
    }
}

/// <summary>
/// Toplu mesaj alıcı detayları
/// </summary>
public class BulkMessageRecipient
{
    public int Id { get; set; }

    public int BulkMessageId { get; set; }

    public virtual BulkMessage BulkMessage { get; set; } = null!;

    public int CustomerId { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    // Hangi kanallardan gönderildi
    public string? SentChannels { get; set; } // JSON array: ["email", "whatsapp"]

    // Gönderim durumu
    public string Status { get; set; } = "Bekliyor"; // Bekliyor, Gönderildi, Hata

    // Maliyet
    public decimal Cost { get; set; } = 0.00m;

    // Zaman bilgileri
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ProcessedAt { get; set; }

    // Hata mesajı
    public string? ErrorMessage { get; set; }

    // Helper properties
    [NotMapped]
    public List<string> SentChannelsList
    {
        get
        {
            if (string.IsNullOrEmpty(SentChannels))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(SentChannels) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        set
        {
            SentChannels = JsonSerializer.Serialize(value);
        }
    }

    [NotMapped]
    public string StatusBadgeClass => Status switch
    {
        "Bekliyor" => "bg-yellow-100 text-yellow-800",
        "Gönderildi" => "bg-green-100 text-green-800",
        "Hata" => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };

    [NotMapped]
    public string FormattedCost => $"₺{Cost:N2}";

    [NotMapped]
    public string FormattedProcessedAt => ProcessedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Henüz işlenmedi";
}
