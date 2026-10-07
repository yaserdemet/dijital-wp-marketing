using PushDashboard.DTOs;

namespace PushDashboard.Services.Modules.VideoHosting;

public interface IVideoHostingService
{
    #region CRUD Operations

    /// <summary>
    /// Uploads a new video and processes it
    /// </summary>
    Task<(bool Success, string Message, Guid? VideoId)> UploadVideoAsync(
        Guid companyId,
        VideoUploadRequestDto request,
        string userId);

    /// <summary>
    /// Gets all videos for a company
    /// </summary>
    Task<List<VideoListDto>> GetVideosAsync(Guid companyId);

    /// <summary>
    /// Gets a specific video by ID
    /// </summary>
    Task<VideoResponseDto?> GetVideoByIdAsync(Guid companyId, Guid videoId);

    /// <summary>
    /// Updates video information
    /// </summary>
    Task<(bool Success, string Message)> UpdateVideoAsync(
        Guid companyId,
        Guid videoId,
        VideoUpdateRequestDto request,
        string userId);

    /// <summary>
    /// Deletes a video
    /// </summary>
    Task<(bool Success, string Message)> DeleteVideoAsync(Guid companyId, Guid videoId, string userId);

    #endregion

    #region Player Operations

    /// <summary>
    /// Gets video player data for viewing
    /// </summary>
    Task<VideoPlayerDto?> GetVideoPlayerDataAsync(Guid companyId, Guid videoId);

    /// <summary>
    /// Gets video player data for public viewing (with access token)
    /// </summary>
    Task<VideoPlayerDto?> GetPublicVideoPlayerDataAsync(Guid videoId, string? accessToken = null, bool? autoPlay = null, bool? muted = null, bool? showControls = null);

    /// <summary>
    /// Records a video view for analytics
    /// </summary>
    Task<(bool Success, string Message)> RecordVideoViewAsync(
        Guid videoId,
        string? viewerIpAddress,
        string? viewerUserAgent,
        string? referrerUrl,
        string? embedUrl = null);

    #endregion

    #region Embed Operations

    /// <summary>
    /// Generates all embed/share codes for a video (iframe src, direct video, video embed, iframe embed)
    /// </summary>
    Task<VideoEmbedDto?> GenerateEmbedCodeAsync(Guid companyId, Guid videoId, int width = 640, int height = 360, bool autoPlay = false, bool muted = false, bool showControls = true);

    /// <summary>
    /// Gets embed player data (for iframe)
    /// </summary>
    Task<VideoPlayerDto?> GetEmbedPlayerDataAsync(Guid videoId, string? domain = null, bool? autoPlay = null, bool? muted = null, bool? showControls = null);

    /// <summary>
    /// Gets the direct video file URL for a public/accessible video
    /// </summary>
    Task<string?> GetDirectVideoUrlAsync(Guid videoId, string? accessToken = null);

    #endregion

    #region Statistics

    /// <summary>
    /// Gets video hosting statistics for a company
    /// </summary>
    Task<VideoStatsDto> GetVideoStatsAsync(Guid companyId);

    /// <summary>
    /// Gets view analytics for a specific video
    /// </summary>
    Task<List<VideoViewAnalyticsDto>> GetVideoViewAnalyticsAsync(Guid companyId, Guid videoId, DateTime? startDate = null, DateTime? endDate = null);

    #endregion

    #region Settings

    /// <summary>
    /// Gets video hosting settings for a company
    /// </summary>
    Task<VideoSettingsDto?> GetSettingsAsync(Guid companyId);

    /// <summary>
    /// Updates video hosting settings for a company
    /// </summary>
    Task<(bool Success, string Message)> UpdateSettingsAsync(
        Guid companyId,
        VideoSettingsRequestDto request,
        string userId);

    #endregion

    #region Validation

    /// <summary>
    /// Validates if company can upload a new video
    /// </summary>
    Task<(bool CanUpload, string Message, decimal RequiredCredits)> ValidateVideoUploadAsync(
        Guid companyId,
        IFormFile videoFile);

    /// <summary>
    /// Calculates upload cost based on video duration
    /// </summary>
    Task<decimal> CalculateUploadCostAsync(Guid companyId, TimeSpan videoDuration);

    #endregion

    #region MB Quota (Package-Based)

    /// <summary>
    /// Checks if the company has enough MB quota in their active package to upload a file of the given size.
    /// </summary>
    Task<(bool HasQuota, string Message, decimal RemainingMB)> CheckVideoQuotaAsync(Guid companyId, decimal requiredMB);

    /// <summary>
    /// Deducts the used MB from the company's active Video Hosting package quota.
    /// </summary>
    Task<bool> DeductVideoQuotaAsync(Guid companyId, decimal usedMB);

    /// <summary>
    /// Returns total/remaining MB quota info for the company's active package.
    /// </summary>
    Task<(bool Found, decimal TotalMB, decimal RemainingMB, string PackageName)> GetQuotaInfoAsync(Guid companyId);

    #endregion
}

/// <summary>
/// Video view analytics DTO
/// </summary>
public class VideoViewAnalyticsDto
{
    public DateTime Date { get; set; }
    public int ViewCount { get; set; }
    public int UniqueViewers { get; set; }
    public TimeSpan AverageWatchDuration { get; set; }
    public int CompletedViews { get; set; }
    public string TopReferrer { get; set; } = string.Empty;
    public string TopCountry { get; set; } = string.Empty;
}
