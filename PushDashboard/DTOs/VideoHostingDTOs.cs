using System.ComponentModel.DataAnnotations;
using PushDashboard.Models;

namespace PushDashboard.DTOs;

#region Request DTOs

public class VideoUploadRequestDto
{
    [Required(ErrorMessage = "Video başlığı gereklidir")]
    [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Video dosyası gereklidir")]
    public IFormFile VideoFile { get; set; } = null!;

    public bool IsPublic { get; set; } = false;
}

public class VideoUpdateRequestDto
{
    [Required(ErrorMessage = "Video başlığı gereklidir")]
    [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
    public string? Description { get; set; }

    public bool IsPublic { get; set; } = false;
}

public class VideoSettingsRequestDto
{
    [Range(1, 5000, ErrorMessage = "Maksimum dosya boyutu 1MB ile 5000MB arasında olmalıdır")]
    public int MaxFileSizeMB { get; set; } = 500;

    [Range(1, 300, ErrorMessage = "Maksimum video süresi 1 ile 300 dakika arasında olmalıdır")]
    public int MaxDurationMinutes { get; set; } = 60;

    [Range(1, 1000, ErrorMessage = "Maksimum video sayısı 1 ile 1000 arasında olmalıdır")]
    public int MaxVideosPerCompany { get; set; } = 100;

    public bool AutoPlay { get; set; } = false;
    public bool ShowControls { get; set; } = true;
    public bool AllowDownload { get; set; } = false;

    [StringLength(50, ErrorMessage = "Player teması en fazla 50 karakter olabilir")]
    public string PlayerTheme { get; set; } = "default";

    [StringLength(5000, ErrorMessage = "Özel CSS en fazla 5000 karakter olabilir")]
    public string? CustomPlayerCss { get; set; }

    public bool RequireAuthentication { get; set; } = false;
    public bool AllowEmbedding { get; set; } = true;

    [StringLength(2000, ErrorMessage = "İzin verilen domain listesi en fazla 2000 karakter olabilir")]
    public string? AllowedDomains { get; set; }
}

#endregion

#region Response DTOs

public class VideoResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string DurationFormatted { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public VideoStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public decimal UploadCost { get; set; }
    public decimal StorageCostPerMonth { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? EmbedCode { get; set; }
    public string? ProcessingError { get; set; }
}

public class VideoListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileSizeFormatted { get; set; } = string.Empty;
    public string DurationFormatted { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public VideoStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int ViewCount { get; set; }
    public decimal UploadCost { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ThumbnailUrl { get; set; }
}

public class VideoPlayerDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public TimeSpan Duration { get; set; }
    public bool AutoPlay { get; set; }
    public bool Muted { get; set; }
    public bool ShowControls { get; set; }
    public bool AllowDownload { get; set; }
    public string PlayerTheme { get; set; } = string.Empty;
    public string? CustomPlayerCss { get; set; }
}

public class VideoEmbedDto
{
    public Guid VideoId { get; set; }

    /// <summary>
    /// 1) iframe src URL'si - sadece URL
    /// </summary>
    public string IframeSrcUrl { get; set; } = string.Empty;

    /// <summary>
    /// 2) Direkt video dosya linki (mp4)
    /// </summary>
    public string DirectVideoUrl { get; set; } = string.Empty;

    /// <summary>
    /// 3) HTML5 video embed kodu (video tag)
    /// </summary>
    public string VideoEmbedCode { get; set; } = string.Empty;

    /// <summary>
    /// 4) iframe embed kodu (tam iframe tag)
    /// </summary>
    public string IframeEmbedCode { get; set; } = string.Empty;

    /// <summary>
    /// Player sayfası (standalone) linki
    /// </summary>
    public string PlayerUrl { get; set; } = string.Empty;

    public int Width { get; set; } = 640;
    public int Height { get; set; } = 360;

    // Seçilen ayarlar
    public bool AutoPlay { get; set; }
    public bool Muted { get; set; }
    public bool ShowControls { get; set; }
}

public class VideoSettingsDto
{
    public decimal CostPerMinute { get; set; }
    public decimal StorageCostPerGBPerMonth { get; set; }
    public int MaxFileSizeMB { get; set; }
    public int MaxDurationMinutes { get; set; }
    public int MaxVideosPerCompany { get; set; }
    public bool AutoPlay { get; set; }
    public bool ShowControls { get; set; }
    public bool AllowDownload { get; set; }
    public string PlayerTheme { get; set; } = string.Empty;
    public string? CustomPlayerCss { get; set; }
    public bool RequireAuthentication { get; set; }
    public bool AllowEmbedding { get; set; }
    public string? AllowedDomains { get; set; }
}

public class VideoStatsDto
{
    public int TotalVideos { get; set; }
    public int ReadyVideos { get; set; }
    public int ProcessingVideos { get; set; }
    public int FailedVideos { get; set; }
    public long TotalStorageBytes { get; set; }
    public string TotalStorageFormatted { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public string TotalDurationFormatted { get; set; } = string.Empty;
    public int TotalViews { get; set; }
    public decimal TotalUploadCosts { get; set; }
    public decimal MonthlyStorageCosts { get; set; }
}

public class VideoUploadProgressDto
{
    public string JobId { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? VideoId { get; set; }
    public bool IsCompleted { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
