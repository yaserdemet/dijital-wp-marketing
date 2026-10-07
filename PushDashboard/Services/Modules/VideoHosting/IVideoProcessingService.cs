namespace PushDashboard.Services.Modules.VideoHosting;

public interface IVideoProcessingService
{
    #region Video Analysis

    /// <summary>
    /// Analyzes video file and extracts metadata (duration, resolution, codecs, etc.)
    /// </summary>
    Task<(bool Success, VideoMetadata Metadata, string Message)> AnalyzeVideoAsync(IFormFile videoFile);

    /// <summary>
    /// Analyzes video file from stream
    /// </summary>
    Task<(bool Success, VideoMetadata Metadata, string Message)> AnalyzeVideoAsync(Stream videoStream, string fileName);

    #endregion

    #region Thumbnail Generation

    /// <summary>
    /// Generates a thumbnail from video at specified time
    /// </summary>
    Task<(bool Success, Stream ThumbnailStream, string Message)> GenerateThumbnailAsync(
        IFormFile videoFile, 
        TimeSpan timeOffset = default);

    /// <summary>
    /// Generates a thumbnail from video stream
    /// </summary>
    Task<(bool Success, Stream ThumbnailStream, string Message)> GenerateThumbnailAsync(
        Stream videoStream, 
        string fileName,
        TimeSpan timeOffset = default);

    #endregion

    #region Validation

    /// <summary>
    /// Validates if the uploaded file is a valid video
    /// </summary>
    Task<(bool IsValid, string Message)> ValidateVideoFileAsync(IFormFile videoFile);

    /// <summary>
    /// Checks if video duration is within allowed limits
    /// </summary>
    bool IsValidDuration(TimeSpan duration, int maxDurationMinutes);

    /// <summary>
    /// Checks if video file size is within allowed limits
    /// </summary>
    bool IsValidFileSize(long fileSizeBytes, long maxFileSizeBytes);

    #endregion

    #region Utility

    /// <summary>
    /// Formats file size to human readable format
    /// </summary>
    string FormatFileSize(long bytes);

    /// <summary>
    /// Formats duration to human readable format
    /// </summary>
    string FormatDuration(TimeSpan duration);

    /// <summary>
    /// Gets supported video formats
    /// </summary>
    string[] GetSupportedVideoFormats();

    #endregion
}

/// <summary>
/// Video metadata extracted from analysis
/// </summary>
public class VideoMetadata
{
    public TimeSpan Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; }
    public string VideoCodec { get; set; } = string.Empty;
    public string AudioCodec { get; set; } = string.Empty;
    public long BitRate { get; set; }
    public string Format { get; set; } = string.Empty;
    public bool HasAudio { get; set; }
    public bool HasVideo { get; set; }
}
