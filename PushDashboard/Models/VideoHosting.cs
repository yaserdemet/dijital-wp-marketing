namespace PushDashboard.Models;

/// <summary>
/// Video hosting entity for storing video information
/// </summary>
public class VideoHosting
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // S3 Information
    public string S3Key { get; set; } = string.Empty; // S3 object key
    public string S3Url { get; set; } = string.Empty; // S3 URL
    public string? ThumbnailS3Key { get; set; }
    public string? ThumbnailS3Url { get; set; }

    // Video Metadata
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; } // Video duration
    public int Width { get; set; }
    public int Height { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public double FrameRate { get; set; }

    // Billing Information
    public decimal UploadCost { get; set; } // Cost charged for upload based on duration
    public decimal StorageCostPerMonth { get; set; } // Monthly storage cost

    // Status
    public VideoStatus Status { get; set; } = VideoStatus.Uploading;
    public string? ProcessingError { get; set; }

    // Access Control
    public bool IsPublic { get; set; } = false;
    public string? AccessToken { get; set; } // For private video access

    // Tracking
    public int ViewCount { get; set; } = 0;
    public DateTime? LastViewedAt { get; set; }

    // Audit
    public string UploadedByUserId { get; set; } = string.Empty;
    public virtual ApplicationUser UploadedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByUserId { get; set; }
    public virtual ApplicationUser? UpdatedByUser { get; set; }

    // Navigation properties
    public virtual ICollection<VideoHostingView> Views { get; set; } = new List<VideoHostingView>();
}

/// <summary>
/// Video status enumeration
/// </summary>
public enum VideoStatus
{
    Uploading = 1,
    Processing = 2,
    Ready = 3,
    Failed = 4,
    Deleted = 5
}

/// <summary>
/// Video view tracking for analytics
/// </summary>
public class VideoHostingView
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VideoHostingId { get; set; }
    public virtual VideoHosting VideoHosting { get; set; } = null!;

    public Guid CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    // Viewer Information
    public string? ViewerIpAddress { get; set; }
    public string? ViewerUserAgent { get; set; }
    public string? ViewerCountry { get; set; }
    public string? ViewerCity { get; set; }

    // View Details
    public DateTime ViewedAt { get; set; } = DateTime.Now;
    public TimeSpan? WatchDuration { get; set; } // How long they watched
    public bool CompletedView { get; set; } = false; // Did they watch till end

    // Referrer Information
    public string? ReferrerUrl { get; set; }
    public string? EmbedUrl { get; set; } // If viewed via embed
}

/// <summary>
/// Company-specific video hosting settings
/// </summary>
public class VideoHostingSettings
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    // Pricing Settings
    public decimal CostPerMinute { get; set; } = 0.5m; // Cost per minute of video
    public decimal StorageCostPerGBPerMonth { get; set; } = 0.1m; // Monthly storage cost per GB

    // Upload Limits
    public long MaxFileSizeBytes { get; set; } = 500 * 1024 * 1024; // 500MB default
    public int MaxDurationMinutes { get; set; } = 60; // 60 minutes default
    public int MaxVideosPerCompany { get; set; } = 100; // 100 videos default

    // Player Settings
    public bool AutoPlay { get; set; } = false;
    public bool ShowControls { get; set; } = true;
    public bool AllowDownload { get; set; } = false;
    public string PlayerTheme { get; set; } = "default"; // default, dark, light
    public string? CustomPlayerCss { get; set; }

    // Security Settings
    public bool RequireAuthentication { get; set; } = false;
    public bool AllowEmbedding { get; set; } = true;
    public string? AllowedDomains { get; set; } // JSON array of allowed domains for embedding

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedByUserId { get; set; } = string.Empty;
    public virtual ApplicationUser UpdatedByUser { get; set; } = null!;
}
