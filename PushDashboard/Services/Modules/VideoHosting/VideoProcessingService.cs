using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using System.Drawing;

namespace PushDashboard.Services.Modules.VideoHosting;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly string[] _supportedFormats = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };

    public VideoProcessingService(ILogger<VideoProcessingService> logger)
    {
        _logger = logger;

        // FFMpeg binary path'lerini konfigüre et
        GlobalFFOptions.Configure(options =>
        {
            // Linux (Docker) için standart path'ler
            if (OperatingSystem.IsLinux())
            {
                options.BinaryFolder = "/usr/bin";
            }
            // macOS için Homebrew path'leri
            else if (OperatingSystem.IsMacOS())
            {
                // Homebrew Intel Mac
                if (Directory.Exists("/usr/local/bin") && File.Exists("/usr/local/bin/ffmpeg"))
                {
                    options.BinaryFolder = "/usr/local/bin";
                }
                // Homebrew Apple Silicon Mac
                else if (Directory.Exists("/opt/homebrew/bin") && File.Exists("/opt/homebrew/bin/ffmpeg"))
                {
                    options.BinaryFolder = "/opt/homebrew/bin";
                }
            }
            // Windows için varsayılan PATH kullanılır

            options.TemporaryFilesFolder = Path.GetTempPath();
        });
    }

    #region Video Analysis

    public async Task<(bool Success, VideoMetadata Metadata, string Message)> AnalyzeVideoAsync(IFormFile videoFile)
    {
        try
        {
            using var stream = videoFile.OpenReadStream();
            return await AnalyzeVideoAsync(stream, videoFile.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing video file: {FileName}", videoFile.FileName);
            return (false, new VideoMetadata(), "Video analizi sırasında hata oluştu.");
        }
    }

    public async Task<(bool Success, VideoMetadata Metadata, string Message)> AnalyzeVideoAsync(Stream inputVideoStream, string fileName)
    {
        try
        {
            // Create a temporary file for FFMpeg analysis
            var tempFilePath = Path.GetTempFileName();
            var tempVideoPath = Path.ChangeExtension(tempFilePath, Path.GetExtension(fileName));

            try
            {
                // Copy stream to temporary file
                using (var fileStream = File.Create(tempVideoPath))
                {
                    inputVideoStream.Position = 0;
                    await inputVideoStream.CopyToAsync(fileStream);
                }

                // Analyze video with FFMpeg
                var mediaInfo = await FFProbe.AnalyseAsync(tempVideoPath);

                var videoStreamInfo = mediaInfo.VideoStreams.FirstOrDefault();
                var audioStreamInfo = mediaInfo.AudioStreams.FirstOrDefault();

                if (videoStreamInfo == null)
                {
                    return (false, new VideoMetadata(), "Video stream bulunamadı.");
                }

                var metadata = new VideoMetadata
                {
                    Duration = mediaInfo.Duration,
                    Width = videoStreamInfo.Width,
                    Height = videoStreamInfo.Height,
                    FrameRate = videoStreamInfo.FrameRate,
                    VideoCodec = videoStreamInfo.CodecName ?? "unknown",
                    AudioCodec = audioStreamInfo?.CodecName ?? "none",
                    BitRate = videoStreamInfo.BitRate,
                    Format = mediaInfo.Format.FormatName ?? "unknown",
                    HasAudio = audioStreamInfo != null,
                    HasVideo = videoStreamInfo != null
                };

                _logger.LogInformation("Video analyzed successfully: {FileName}, Duration: {Duration}, Resolution: {Width}x{Height}",
                    fileName, metadata.Duration, metadata.Width, metadata.Height);

                return (true, metadata, "Video başarıyla analiz edildi.");
            }
            finally
            {
                // Clean up temporary files
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                if (File.Exists(tempVideoPath))
                    File.Delete(tempVideoPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing video stream: {FileName}", fileName);
            return (false, new VideoMetadata(), "Video analizi sırasında hata oluştu.");
        }
    }

    #endregion

    #region Thumbnail Generation

    public async Task<(bool Success, Stream ThumbnailStream, string Message)> GenerateThumbnailAsync(
        IFormFile videoFile, 
        TimeSpan timeOffset = default)
    {
        try
        {
            using var stream = videoFile.OpenReadStream();
            return await GenerateThumbnailAsync(stream, videoFile.FileName, timeOffset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for video file: {FileName}", videoFile.FileName);
            return (false, Stream.Null, "Thumbnail oluşturma sırasında hata oluştu.");
        }
    }

    public async Task<(bool Success, Stream ThumbnailStream, string Message)> GenerateThumbnailAsync(
        Stream inputVideoStream,
        string fileName,
        TimeSpan timeOffset = default)
    {
        var tempVideoPath = Path.Combine(Path.GetTempPath(), $"video_{Guid.NewGuid()}{Path.GetExtension(fileName)}");
        var tempThumbnailPath = Path.Combine(Path.GetTempPath(), $"thumb_{Guid.NewGuid()}.jpg");

        try
        {
            _logger.LogInformation("Starting thumbnail generation for: {FileName}", fileName);
            _logger.LogInformation("Temp video path: {TempVideoPath}", tempVideoPath);
            _logger.LogInformation("Temp thumbnail path: {TempThumbnailPath}", tempThumbnailPath);

            // Save video temporarily
            if (inputVideoStream.CanSeek)
            {
                inputVideoStream.Position = 0;
            }

            using (var fileStream = new FileStream(tempVideoPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await inputVideoStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }

            // Verify video file was created
            if (!File.Exists(tempVideoPath))
            {
                _logger.LogError("Temp video file was not created: {TempVideoPath}", tempVideoPath);
                return (false, Stream.Null, "Geçici video dosyası oluşturulamadı.");
            }

            var videoFileInfo = new FileInfo(tempVideoPath);
            _logger.LogInformation("Temp video file created. Size: {Size} bytes", videoFileInfo.Length);

            // Analyze video to get duration
            var mediaInfo = await FFProbe.AnalyseAsync(tempVideoPath);
            _logger.LogInformation("Video analyzed. Duration: {Duration}", mediaInfo.Duration);

            if (timeOffset == default)
            {
                var tenPercent = TimeSpan.FromSeconds(mediaInfo.Duration.TotalSeconds * 0.1);
                timeOffset = tenPercent > TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : tenPercent;

                // Minimum 1 saniye
                if (timeOffset < TimeSpan.FromSeconds(1))
                {
                    timeOffset = TimeSpan.FromSeconds(1);
                }
            }

            _logger.LogInformation("Generating thumbnail at offset: {TimeOffset}", timeOffset);

            // FFMpegArguments kullanarak daha güvenilir thumbnail oluşturma
            var snapshotResult = await FFMpegArguments
                .FromFileInput(tempVideoPath, verifyExists: true, delegate(FFMpegArgumentOptions options)
                {
                    options.Seek(timeOffset);
                })
                .OutputToFile(tempThumbnailPath, overwrite: true, delegate(FFMpegArgumentOptions options)
                {
                    options
                        .WithVideoCodec("mjpeg")
                        .WithFrameOutputCount(1)
                        .WithCustomArgument("-q:v 2"); // Yüksek kalite
                })
                .ProcessAsynchronously();

            _logger.LogInformation("FFMpeg process completed. Result: {Result}", snapshotResult);

            // Dosyanın oluşup oluşmadığını kontrol et
            if (!File.Exists(tempThumbnailPath))
            {
                _logger.LogError("Thumbnail file was not created at: {TempThumbnailPath}", tempThumbnailPath);

                // Alternatif yöntem: FFMpeg.SnapshotAsync dene
                _logger.LogInformation("Trying alternative method: FFMpeg.SnapshotAsync");
                var altSuccess = await FFMpeg.SnapshotAsync(tempVideoPath, tempThumbnailPath, null, timeOffset);

                if (!altSuccess || !File.Exists(tempThumbnailPath))
                {
                    _logger.LogError("Alternative method also failed");
                    return (false, Stream.Null, "FFmpeg thumbnail dosyası oluşturamadı.");
                }
            }

            var thumbFileInfo = new FileInfo(tempThumbnailPath);
            _logger.LogInformation("Thumbnail file created. Size: {Size} bytes", thumbFileInfo.Length);

            // Read thumbnail into memory
            var thumbnailBytes = await File.ReadAllBytesAsync(tempThumbnailPath);
            var thumbnailStream = new MemoryStream(thumbnailBytes);

            _logger.LogInformation("Thumbnail generated successfully for video: {FileName} at {TimeOffset}, Size: {Size} bytes",
                fileName, timeOffset, thumbnailBytes.Length);

            return (true, thumbnailStream, "Thumbnail başarıyla oluşturuldu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for video: {FileName}. Exception: {Message}", fileName, ex.Message);
            return (false, Stream.Null, $"Thumbnail oluşturma sırasında hata oluştu: {ex.Message}");
        }
        finally
        {
            // Clean up temporary files
            try
            {
                if (File.Exists(tempVideoPath))
                {
                    File.Delete(tempVideoPath);
                    _logger.LogDebug("Deleted temp video file: {TempVideoPath}", tempVideoPath);
                }
                if (File.Exists(tempThumbnailPath))
                {
                    File.Delete(tempThumbnailPath);
                    _logger.LogDebug("Deleted temp thumbnail file: {TempThumbnailPath}", tempThumbnailPath);
                }
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Error cleaning up temporary files");
            }
        }
    }

    #endregion

    #region Validation

    public async Task<(bool IsValid, string Message)> ValidateVideoFileAsync(IFormFile videoFile)
    {
        try
        {
            // Check file extension
            var extension = Path.GetExtension(videoFile.FileName).ToLowerInvariant();
            if (!_supportedFormats.Contains(extension))
            {
                return (false, $"Desteklenmeyen video formatı: {extension}. Desteklenen formatlar: {string.Join(", ", _supportedFormats)}");
            }

            // Check content type
            if (!videoFile.ContentType.StartsWith("video/"))
            {
                return (false, "Dosya video formatında değil.");
            }

            // Try to analyze the video to ensure it's valid
            var analysisResult = await AnalyzeVideoAsync(videoFile);
            if (!analysisResult.Success)
            {
                return (false, "Video dosyası geçersiz veya bozuk.");
            }

            return (true, "Video dosyası geçerli.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating video file: {FileName}", videoFile.FileName);
            return (false, "Video dosyası doğrulama sırasında hata oluştu.");
        }
    }

    public bool IsValidDuration(TimeSpan duration, int maxDurationMinutes)
    {
        return duration.TotalMinutes <= maxDurationMinutes && duration.TotalSeconds > 0;
    }

    public bool IsValidFileSize(long fileSizeBytes, long maxFileSizeBytes)
    {
        return fileSizeBytes <= maxFileSizeBytes && fileSizeBytes > 0;
    }

    #endregion

    #region Utility

    public string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
        else
        {
            return $"{duration.Minutes}:{duration.Seconds:D2}";
        }
    }

    public string[] GetSupportedVideoFormats()
    {
        return _supportedFormats;
    }

    #endregion
}
