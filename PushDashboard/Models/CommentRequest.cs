using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

public class CommentRequest
{
    public int Id { get; set; }

    // Hangi şirkete ait olduğu
    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public string ProductUrl { get; set; } = string.Empty;

    public string ExternalProductId { get; set; } = string.Empty;

    public string? ExternalProductUrl { get; set; }

    public string ReviewSource { get; set; } = "Trendyol";

    public int RequestedCommentsCount { get; set; } = 1000;

    public int? ActualCommentsCount { get; set; }

    public string Status { get; set; } = "Sırada"; // Sırada, İşleniyor, Hazır, Hata

    public string? CommentsFileUrl { get; set; }

    public string? LogsFileUrl { get; set; }

    public string? ScreenshotUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public DateTime? ProcessedAt { get; set; }

    public string ExportToken { get; set; } = Guid.NewGuid().ToString();

    // Webhook related fields
    public string? WebhookUrl { get; set; }

    public string? ExternalRequestId { get; set; }

    public bool IsProcessing { get; set; } = false;

    // Helper properties
    [NotMapped]
    public string StatusBadgeClass => Status switch
    {
        "Sırada" => "bg-gray-100 text-gray-800",
        "İşleniyor" => "bg-blue-100 text-blue-800",
        "Hazır" => "bg-green-100 text-green-800",
        "Hata" => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };

    [NotMapped]
    public string StatusIcon => Status switch
    {
        "Sırada" => "clock",
        "İşleniyor" => "refresh-cw",
        "Hazır" => "check-circle",
        "Hata" => "x-circle",
        _ => "clock"
    };

    [NotMapped]
    public string ShortProductUrl
    {
        get
        {
            if (string.IsNullOrEmpty(ProductUrl)) return "";

            try
            {
                var uri = new Uri(ProductUrl);
                var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (pathSegments.Length > 0)
                {
                    var lastSegment = pathSegments.Last();
                    return lastSegment.Length > 50 ? lastSegment.Substring(0, 50) + "..." : lastSegment;
                }
                return uri.Host;
            }
            catch
            {
                return ProductUrl.Length > 50 ? ProductUrl.Substring(0, 50) + "..." : ProductUrl;
            }
        }
    }

    // Status mapping helper methods
    public static string MapExternalStatusToInternal(string externalStatus)
    {
        return externalStatus?.ToLower() switch
        {
            "pending" => "Sırada",
            "running" => "İşleniyor",
            "completed" => "Hazır",
            "failed" => "Hata",
            _ => "Sırada"
        };
    }

    public static string MapInternalStatusToExternal(string internalStatus)
    {
        return internalStatus switch
        {
            "Sırada" => "pending",
            "İşleniyor" => "running",
            "Hazır" => "completed",
            "Hata" => "failed",
            _ => "pending"
        };
    }
}

public class Comment
{
    public string CommentText { get; set; } = string.Empty;
    public string Info { get; set; } = string.Empty;
}
