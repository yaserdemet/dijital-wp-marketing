using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Models;

namespace PushDashboard.Services.Modules.VideoHosting;

public class VideoHostingService : IVideoHostingService
{
    private readonly ApplicationDbContext _context;
    private readonly IR2StorageService _r2StorageService;
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly ILogger<VideoHostingService> _logger;

    public VideoHostingService(
        ApplicationDbContext context,
        IR2StorageService r2StorageService,
        IVideoProcessingService videoProcessingService,
        IModuleUsageService moduleUsageService,
        ILogger<VideoHostingService> logger)
    {
        _context = context;
        _r2StorageService = r2StorageService;
        _videoProcessingService = videoProcessingService;
        _moduleUsageService = moduleUsageService;
        _logger = logger;
    }

    #region CRUD Operations

    public async Task<(bool Success, string Message, Guid? VideoId)> UploadVideoAsync(
        Guid companyId,
        VideoUploadRequestDto request,
        string userId)
    {
        try
        {
            // Validate video upload
            var validationResult = await ValidateVideoUploadAsync(companyId, request.VideoFile);
            if (!validationResult.CanUpload)
            {
                return (false, validationResult.Message, null);
            }

            // Analyze video
            var analysisResult = await _videoProcessingService.AnalyzeVideoAsync(request.VideoFile);
            if (!analysisResult.Success)
            {
                return (false, analysisResult.Message, null);
            }

            var metadata = analysisResult.Metadata;

            // Calculate upload size in MB
            var fileSizeMB = Math.Round(request.VideoFile.Length / (1024m * 1024m), 2);

            // Check if company has enough MB quota in active package
            var quotaCheck = await CheckVideoQuotaAsync(companyId, fileSizeMB);
            if (!quotaCheck.HasQuota)
            {
                return (false, quotaCheck.Message, null);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create video entity
                var video = new Models.VideoHosting
                {
                    CompanyId = companyId,
                    Title = request.Title,
                    Description = request.Description,
                    OriginalFileName = request.VideoFile.FileName,
                    FileSizeBytes = request.VideoFile.Length,
                    ContentType = request.VideoFile.ContentType,
                    Duration = metadata.Duration,
                    Width = metadata.Width,
                    Height = metadata.Height,
                    VideoCodec = metadata.VideoCodec,
                    AudioCodec = metadata.AudioCodec,
                    FrameRate = metadata.FrameRate,
                    UploadCost = 0,  // Kredi kesimi kaldırıldı — MB kotası düşülür
                    Status = VideoStatus.Uploading,
                    IsPublic = request.IsPublic,
                    AccessToken = request.IsPublic ? null : Guid.NewGuid().ToString("N"),
                    UploadedByUserId = userId,
                    CreatedAt = DateTime.Now
                };

                _context.VideoHostings.Add(video);
                await _context.SaveChangesAsync();

                // Upload to R2 Storage
                var fileName = $"{video.Id}_{request.VideoFile.FileName}";
                var r2Key = $"videos/{companyId}/{fileName}";

                var videoMetadata = new Dictionary<string, string>
                {
                    ["company-id"] = companyId.ToString(),
                    ["video-id"] = video.Id.ToString(),
                    ["original-filename"] = request.VideoFile.FileName,
                    ["upload-timestamp"] = DateTime.Now.ToString("O"),
                    ["duration-seconds"] = analysisResult.Metadata.Duration.TotalSeconds.ToString(),
                    ["width"] = analysisResult.Metadata.Width.ToString(),
                    ["height"] = analysisResult.Metadata.Height.ToString()
                };

                try
                {
                    var r2Url = await _r2StorageService.UploadVideoAsync(
                        r2Key,
                        request.VideoFile.OpenReadStream(),
                        request.VideoFile.ContentType,
                        videoMetadata);

                    // Update video with R2 information
                    video.S3Key = r2Key; // Keep same field name for compatibility
                    video.S3Url = r2Url; // Keep same field name for compatibility
                    video.Status = VideoStatus.Processing;
                }
                catch (Exception ex)
                {
                    video.Status = VideoStatus.Failed;
                    video.ProcessingError = ex.Message;
                    await _context.SaveChangesAsync();
                    await transaction.RollbackAsync();
                    return (false, $"Video yükleme hatası: {ex.Message}", null);
                }

                // Generate thumbnail
                var thumbnailResult = await _videoProcessingService.GenerateThumbnailAsync(request.VideoFile);
                if (thumbnailResult.Success)
                {
                    try
                    {
                        var thumbnailKey = $"thumbnails/{companyId}/{video.Id}_thumbnail.jpg";
                        var thumbnailMetadata = new Dictionary<string, string>
                        {
                            ["company-id"] = companyId.ToString(),
                            ["video-id"] = video.Id.ToString(),
                            ["thumbnail-for"] = fileName,
                            ["upload-timestamp"] = DateTime.Now.ToString("O")
                        };

                        var thumbnailUrl = await _r2StorageService.UploadImageAsync(
                            thumbnailKey,
                            thumbnailResult.ThumbnailStream,
                            "image/jpeg",
                            thumbnailMetadata);

                        video.ThumbnailS3Key = thumbnailKey; // Keep same field name for compatibility
                        video.ThumbnailS3Url = thumbnailUrl; // Keep same field name for compatibility
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Thumbnail upload failed for video {VideoId}", video.Id);
                        // Continue without thumbnail - not critical
                    }

                    thumbnailResult.ThumbnailStream.Dispose();
                }

                // Calculate storage cost
                var settings = await GetOrCreateSettingsAsync(companyId, userId);
                video.StorageCostPerMonth = (decimal)(video.FileSizeBytes / (1024.0 * 1024.0 * 1024.0)) * settings.StorageCostPerGBPerMonth;

                video.Status = VideoStatus.Ready;
                video.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Deduct MB quota from active package (outer fileSizeMB is already in scope)
                var deducted = await DeductVideoQuotaAsync(companyId, fileSizeMB);
                if (!deducted)
                {
                    _logger.LogWarning("Could not deduct quota for company {CompanyId} after video upload. Video: {VideoId}", companyId, video.Id);
                    // Non-fatal: video already uploaded; log and continue
                }

                _logger.LogInformation("Video uploaded successfully. Company: {CompanyId}, VideoId: {VideoId}, SizeMB: {SizeMB}",
                    companyId, video.Id, fileSizeMB);

                return (true, "Video başarıyla yüklendi ve işlendi.", video.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during video upload transaction. Company: {CompanyId}", companyId);
                return (false, "Video yükleme sırasında hata oluştu.", null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video. Company: {CompanyId}", companyId);
            return (false, "Video yükleme sırasında hata oluştu.", null);
        }
    }

    public async Task<List<VideoListDto>> GetVideosAsync(Guid companyId)
    {
        try
        {
            var videos = await _context.VideoHostings
                .Where(v => v.CompanyId == companyId)
                .OrderByDescending(v => v.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return videos.Select(v => new VideoListDto
            {
                Id = v.Id,
                Title = v.Title,
                OriginalFileName = v.OriginalFileName,
                FileSizeFormatted = _videoProcessingService.FormatFileSize(v.FileSizeBytes),
                DurationFormatted = _videoProcessingService.FormatDuration(v.Duration),
                Resolution = $"{v.Width}x{v.Height}",
                Status = v.Status,
                StatusText = GetStatusText(v.Status),
                IsPublic = v.IsPublic,
                ViewCount = v.ViewCount,
                UploadCost = v.UploadCost,
                CreatedAt = v.CreatedAt,
                ThumbnailUrl = v.ThumbnailS3Url
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting videos for company {CompanyId}", companyId);
            return new List<VideoListDto>();
        }
    }

    public async Task<VideoResponseDto?> GetVideoByIdAsync(Guid companyId, Guid videoId)
    {
        try
        {
            var video = await _context.VideoHostings
                .Include(v => v.UploadedByUser)
                .Where(v => v.CompanyId == companyId && v.Id == videoId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (video == null)
                return null;

            // Generate presigned URL for video access
            string? videoUrl = null;
            if (video.Status == VideoStatus.Ready && !string.IsNullOrEmpty(video.S3Key))
            {
                // R2Storage provides direct public URLs
                videoUrl = _r2StorageService.GetPublicUrl(video.S3Key);
            }

            // Generate embed code
            var embedCode = GenerateIframeEmbedCode(video.Id, 640, 360);

            return new VideoResponseDto
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                OriginalFileName = video.OriginalFileName,
                FileSizeBytes = video.FileSizeBytes,
                FileSizeFormatted = _videoProcessingService.FormatFileSize(video.FileSizeBytes),
                Duration = video.Duration,
                DurationFormatted = _videoProcessingService.FormatDuration(video.Duration),
                Width = video.Width,
                Height = video.Height,
                Resolution = $"{video.Width}x{video.Height}",
                Status = video.Status,
                StatusText = GetStatusText(video.Status),
                IsPublic = video.IsPublic,
                ViewCount = video.ViewCount,
                LastViewedAt = video.LastViewedAt,
                UploadCost = video.UploadCost,
                StorageCostPerMonth = video.StorageCostPerMonth,
                CreatedAt = video.CreatedAt,
                UploadedByUserName = video.UploadedByUser?.UserName ?? "Unknown",
                ThumbnailUrl = video.ThumbnailS3Url,
                VideoUrl = videoUrl,
                EmbedCode = embedCode,
                ProcessingError = video.ProcessingError
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video {VideoId} for company {CompanyId}", videoId, companyId);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> UpdateVideoAsync(
        Guid companyId,
        Guid videoId,
        VideoUpdateRequestDto request,
        string userId)
    {
        try
        {
            var video = await _context.VideoHostings
                .FirstOrDefaultAsync(v => v.CompanyId == companyId && v.Id == videoId);

            if (video == null)
            {
                return (false, "Video bulunamadı.");
            }

            video.Title = request.Title;
            video.Description = request.Description;
            video.IsPublic = request.IsPublic;
            video.UpdatedAt = DateTime.Now;
            video.UpdatedByUserId = userId;

            // If changing from public to private, generate access token
            if (!request.IsPublic && video.AccessToken == null)
            {
                video.AccessToken = Guid.NewGuid().ToString("N");
            }
            // If changing from private to public, remove access token
            else if (request.IsPublic)
            {
                video.AccessToken = null;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Video updated successfully. Company: {CompanyId}, VideoId: {VideoId}",
                companyId, videoId);

            return (true, "Video başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating video {VideoId} for company {CompanyId}", videoId, companyId);
            return (false, "Video güncelleme sırasında hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteVideoAsync(Guid companyId, Guid videoId, string userId)
    {
        try
        {
            var video = await _context.VideoHostings
                .FirstOrDefaultAsync(v => v.CompanyId == companyId && v.Id == videoId);

            if (video == null)
            {
                return (false, "Video bulunamadı.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Delete from R2 Storage
                if (!string.IsNullOrEmpty(video.S3Key))
                {
                    try
                    {
                        await _r2StorageService.DeleteFileAsync(video.S3Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete video file from R2: {S3Key}", video.S3Key);
                        // Continue with database deletion even if R2 deletion fails
                    }
                }

                if (!string.IsNullOrEmpty(video.ThumbnailS3Key))
                {
                    try
                    {
                        await _r2StorageService.DeleteFileAsync(video.ThumbnailS3Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete thumbnail file from R2: {ThumbnailS3Key}", video.ThumbnailS3Key);
                        // Continue with database deletion even if R2 deletion fails
                    }
                }

                // Mark as deleted in database
                video.Status = VideoStatus.Deleted;
                video.UpdatedAt = DateTime.Now;
                video.UpdatedByUserId = userId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Video deleted successfully. Company: {CompanyId}, VideoId: {VideoId}",
                    companyId, videoId);

                return (true, "Video başarıyla silindi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during video deletion transaction. Company: {CompanyId}, VideoId: {VideoId}",
                    companyId, videoId);
                return (false, "Video silme sırasında hata oluştu.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting video {VideoId} for company {CompanyId}", videoId, companyId);
            return (false, "Video silme sırasında hata oluştu.");
        }
    }

    #endregion

    #region Player Operations

    public async Task<VideoPlayerDto?> GetVideoPlayerDataAsync(Guid companyId, Guid videoId)
    {
        try
        {
            var video = await _context.VideoHostings
                .Where(v => v.CompanyId == companyId && v.Id == videoId && v.Status == VideoStatus.Ready)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (video == null)
                return null;

            var settings = await _context.VideoHostingSettings
                .Where(s => s.CompanyId == companyId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // Generate public URL for R2Storage
            if (string.IsNullOrEmpty(video.S3Key))
                return null;

            var videoUrl = _r2StorageService.GetPublicUrl(video.S3Key);

            return new VideoPlayerDto
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                VideoUrl = videoUrl,
                ThumbnailUrl = video.ThumbnailS3Url,
                Width = video.Width,
                Height = video.Height,
                Duration = video.Duration,
                AutoPlay = settings?.AutoPlay ?? false,
                Muted = false,
                ShowControls = settings?.ShowControls ?? true,
                AllowDownload = settings?.AllowDownload ?? false,
                PlayerTheme = settings?.PlayerTheme ?? "default",
                CustomPlayerCss = settings?.CustomPlayerCss
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video player data for video {VideoId}, company {CompanyId}",
                videoId, companyId);
            return null;
        }
    }

    #endregion

    #region Validation

    public async Task<(bool CanUpload, string Message, decimal RequiredCredits)> ValidateVideoUploadAsync(
        Guid companyId,
        IFormFile videoFile)
    {
        try
        {
            // Validate file
            var fileValidation = await _videoProcessingService.ValidateVideoFileAsync(videoFile);
            if (!fileValidation.IsValid)
            {
                return (false, fileValidation.Message, 0);
            }

            // Get settings
            var settings = await _context.VideoHostingSettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (settings != null)
            {
                // Check file size limit
                if (!_videoProcessingService.IsValidFileSize(videoFile.Length, settings.MaxFileSizeBytes))
                {
                    var maxSizeMB = settings.MaxFileSizeBytes / (1024 * 1024);
                    return (false, $"Dosya boyutu çok büyük. Maksimum: {maxSizeMB} MB", 0);
                }

                // Check video count limit
                var currentVideoCount = await _context.VideoHostings
                    .CountAsync(v => v.CompanyId == companyId && v.Status != VideoStatus.Deleted);

                if (currentVideoCount >= settings.MaxVideosPerCompany)
                {
                    return (false, $"Maksimum video sayısına ulaştınız. Limit: {settings.MaxVideosPerCompany}", 0);
                }
            }

            // Analyze video for duration check
            var analysisResult = await _videoProcessingService.AnalyzeVideoAsync(videoFile);
            if (!analysisResult.Success)
            {
                return (false, "Video analizi başarısız.", 0);
            }

            var maxDurationMinutes = settings?.MaxDurationMinutes ?? 60;
            if (!_videoProcessingService.IsValidDuration(analysisResult.Metadata.Duration, maxDurationMinutes))
            {
                return (false, $"Video süresi çok uzun. Maksimum: {maxDurationMinutes} dakika", 0);
            }

            // Calculate required credits
            var requiredCredits = await CalculateUploadCostAsync(companyId, analysisResult.Metadata.Duration);

            return (true, "Video yükleme için uygun.", requiredCredits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating video upload for company {CompanyId}", companyId);
            return (false, "Video doğrulama sırasında hata oluştu.", 0);
        }
    }

    public async Task<decimal> CalculateUploadCostAsync(Guid companyId, TimeSpan videoDuration)
    {
        try
        {
            var settings = await _context.VideoHostingSettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            var costPerMinute = settings?.CostPerMinute ?? 0.5m;
            var durationMinutes = (decimal)videoDuration.TotalMinutes;

            return Math.Ceiling(durationMinutes) * costPerMinute;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating upload cost for company {CompanyId}", companyId);
            return 0.5m; // Default cost
        }
    }

    #endregion

    #region Settings

    public async Task<VideoSettingsDto?> GetSettingsAsync(Guid companyId)
    {
        try
        {
            var settings = await _context.VideoHostingSettings
                .Where(s => s.CompanyId == companyId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (settings == null)
                return null;

            return new VideoSettingsDto
            {
                CostPerMinute = settings.CostPerMinute,
                StorageCostPerGBPerMonth = settings.StorageCostPerGBPerMonth,
                MaxFileSizeMB = (int)(settings.MaxFileSizeBytes / (1024 * 1024)),
                MaxDurationMinutes = settings.MaxDurationMinutes,
                MaxVideosPerCompany = settings.MaxVideosPerCompany,
                AutoPlay = settings.AutoPlay,
                ShowControls = settings.ShowControls,
                AllowDownload = settings.AllowDownload,
                PlayerTheme = settings.PlayerTheme,
                CustomPlayerCss = settings.CustomPlayerCss,
                RequireAuthentication = settings.RequireAuthentication,
                AllowEmbedding = settings.AllowEmbedding,
                AllowedDomains = settings.AllowedDomains
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings for company {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> UpdateSettingsAsync(
        Guid companyId,
        VideoSettingsRequestDto request,
        string userId)
    {
        try
        {
            var settings = await GetOrCreateSettingsAsync(companyId, userId);

            // Fiyatlandırma ayarları kullanıcı tarafından değiştirilemez - sabit değerler kullanılır
            // settings.CostPerMinute ve settings.StorageCostPerGBPerMonth değiştirilmez

            settings.MaxFileSizeBytes = request.MaxFileSizeMB * 1024 * 1024;
            settings.MaxDurationMinutes = request.MaxDurationMinutes;
            settings.MaxVideosPerCompany = request.MaxVideosPerCompany;
            settings.AutoPlay = request.AutoPlay;
            settings.ShowControls = request.ShowControls;
            settings.AllowDownload = request.AllowDownload;
            settings.PlayerTheme = request.PlayerTheme;
            settings.CustomPlayerCss = request.CustomPlayerCss;
            settings.RequireAuthentication = request.RequireAuthentication;
            settings.AllowEmbedding = request.AllowEmbedding;
            settings.AllowedDomains = request.AllowedDomains;
            settings.UpdatedAt = DateTime.Now;
            settings.UpdatedByUserId = userId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Video hosting settings updated for company {CompanyId}", companyId);

            return (true, "Ayarlar başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings for company {CompanyId}", companyId);
            return (false, "Ayarlar güncelleme sırasında hata oluştu.");
        }
    }

    #endregion

    #region Statistics and Analytics

    public async Task<VideoStatsDto> GetVideoStatsAsync(Guid companyId)
    {
        try
        {
            var videos = await _context.VideoHostings
                .Where(v => v.CompanyId == companyId && v.Status != VideoStatus.Deleted)
                .AsNoTracking()
                .ToListAsync();

            var totalViews = await _context.VideoHostingViews
                .Where(vv => vv.CompanyId == companyId)
                .CountAsync();

            return new VideoStatsDto
            {
                TotalVideos = videos.Count,
                ReadyVideos = videos.Count(v => v.Status == VideoStatus.Ready),
                ProcessingVideos = videos.Count(v => v.Status == VideoStatus.Processing || v.Status == VideoStatus.Uploading),
                FailedVideos = videos.Count(v => v.Status == VideoStatus.Failed),
                TotalStorageBytes = videos.Sum(v => v.FileSizeBytes),
                TotalStorageFormatted = _videoProcessingService.FormatFileSize(videos.Sum(v => v.FileSizeBytes)),
                TotalDuration = TimeSpan.FromTicks(videos.Sum(v => v.Duration.Ticks)),
                TotalDurationFormatted = _videoProcessingService.FormatDuration(TimeSpan.FromTicks(videos.Sum(v => v.Duration.Ticks))),
                TotalViews = totalViews,
                TotalUploadCosts = videos.Sum(v => v.UploadCost),
                MonthlyStorageCosts = videos.Sum(v => v.StorageCostPerMonth)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video stats for company {CompanyId}", companyId);
            return new VideoStatsDto();
        }
    }

    public async Task<List<VideoViewAnalyticsDto>> GetVideoViewAnalyticsAsync(Guid companyId, Guid videoId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.VideoHostingViews
                .Where(vv => vv.CompanyId == companyId && vv.VideoHostingId == videoId);

            if (startDate.HasValue)
                query = query.Where(vv => vv.ViewedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(vv => vv.ViewedAt <= endDate.Value);

            var views = await query
                .AsNoTracking()
                .ToListAsync();

            var analytics = views
                .GroupBy(v => v.ViewedAt.Date)
                .Select(g => new VideoViewAnalyticsDto
                {
                    Date = g.Key,
                    ViewCount = g.Count(),
                    UniqueViewers = g.GroupBy(v => v.ViewerIpAddress).Count(),
                    AverageWatchDuration = TimeSpan.FromTicks((long)g.Where(v => v.WatchDuration.HasValue).Average(v => v.WatchDuration?.Ticks ?? 0)),
                    CompletedViews = g.Count(v => v.CompletedView),
                    TopReferrer = g.GroupBy(v => v.ReferrerUrl).OrderByDescending(rg => rg.Count()).FirstOrDefault()?.Key ?? "",
                    TopCountry = g.GroupBy(v => v.ViewerCountry).OrderByDescending(cg => cg.Count()).FirstOrDefault()?.Key ?? ""
                })
                .OrderBy(a => a.Date)
                .ToList();

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video analytics for video {VideoId}, company {CompanyId}", videoId, companyId);
            return new List<VideoViewAnalyticsDto>();
        }
    }

    public async Task<(bool Success, string Message)> RecordVideoViewAsync(
        Guid videoId,
        string? viewerIpAddress,
        string? viewerUserAgent,
        string? referrerUrl,
        string? embedUrl = null)
    {
        try
        {
            var video = await _context.VideoHostings
                .FirstOrDefaultAsync(v => v.Id == videoId && v.Status == VideoStatus.Ready);

            if (video == null)
            {
                return (false, "Video bulunamadı.");
            }

            var view = new VideoHostingView
            {
                VideoHostingId = videoId,
                CompanyId = video.CompanyId,
                ViewerIpAddress = viewerIpAddress,
                ViewerUserAgent = viewerUserAgent,
                ReferrerUrl = referrerUrl,
                EmbedUrl = embedUrl,
                ViewedAt = DateTime.Now
            };

            _context.VideoHostingViews.Add(view);

            // Update video view count and last viewed date
            video.ViewCount++;
            video.LastViewedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return (true, "Video görüntüleme kaydedildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording video view for video {VideoId}", videoId);
            return (false, "Video görüntüleme kaydı sırasında hata oluştu.");
        }
    }

    #endregion

    #region Embed Operations

    public async Task<VideoEmbedDto?> GenerateEmbedCodeAsync(Guid companyId, Guid videoId, int width = 640, int height = 360, bool autoPlay = false, bool muted = false, bool showControls = true)
    {
        try
        {
            var video = await _context.VideoHostings
                .Where(v => v.CompanyId == companyId && v.Id == videoId && v.Status == VideoStatus.Ready)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (video == null)
                return null;

            var settings = await _context.VideoHostingSettings
                .Where(s => s.CompanyId == companyId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (settings?.AllowEmbedding == false)
                return null;

            var baseUrl = "https://admin.pushonica.com"; // This should come from configuration

            // Build query string for player settings
            var queryParams = new List<string>();
            if (autoPlay) queryParams.Add("autoplay=1");
            if (muted) queryParams.Add("muted=1");
            if (!showControls) queryParams.Add("controls=0");

            // Add dimensions to URL if they differ from defaults or are specifically requested
            if (width > 0) queryParams.Add($"w={width}");
            if (height > 0) queryParams.Add($"h={height}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";

            // 1) iframe src URL
            var iframeSrcUrl = $"{baseUrl}/VideoHosting/Embed/{videoId}{queryString}";

            // 2) Direct video file URL (mp4)
            var directVideoUrl = !string.IsNullOrEmpty(video.S3Key)
                ? _r2StorageService.GetPublicUrl(video.S3Key)
                : $"{baseUrl}/VideoHosting/DirectVideo/{videoId}";

            // 3) HTML5 video embed code
            var videoAttrs = new List<string>();
            if (showControls) videoAttrs.Add("controls");
            if (autoPlay) videoAttrs.Add("autoplay");
            if (muted) videoAttrs.Add("muted");
            videoAttrs.Add("playsinline");
            videoAttrs.Add("preload=\"metadata\"");
            var videoAttrsStr = string.Join(" ", videoAttrs);

            var posterAttr = !string.IsNullOrEmpty(video.ThumbnailS3Url)
                ? $" poster=\"{video.ThumbnailS3Url}\""
                : "";

            var videoEmbedCode = $"<video width=\"{width}\" height=\"{height}\" {videoAttrsStr}{posterAttr} style=\"max-width:100%;height:auto;\">\n  <source src=\"{directVideoUrl}\" type=\"video/mp4\">\n  Tarayıcınız video oynatmayı desteklemiyor.\n</video>";

            // 4) iframe embed code
            var iframeEmbedCode = $"<iframe src=\"{iframeSrcUrl}\" width=\"{width}\" height=\"{height}\" frameborder=\"0\" allow=\"autoplay; fullscreen; picture-in-picture\" allowfullscreen style=\"max-width:100%;border:none;\"></iframe>";

            // Player page URL (standalone)
            var playerUrl = $"{baseUrl}/VideoHosting/Player/{videoId}{queryString}";

            return new VideoEmbedDto
            {
                VideoId = videoId,
                IframeSrcUrl = iframeSrcUrl,
                DirectVideoUrl = directVideoUrl,
                VideoEmbedCode = videoEmbedCode,
                IframeEmbedCode = iframeEmbedCode,
                PlayerUrl = playerUrl,
                Width = width,
                Height = height,
                AutoPlay = autoPlay,
                Muted = muted,
                ShowControls = showControls
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embed code for video {VideoId}, company {CompanyId}", videoId, companyId);
            return null;
        }
    }

    public async Task<VideoPlayerDto?> GetEmbedPlayerDataAsync(Guid videoId, string? domain = null, bool? autoPlay = null, bool? muted = null, bool? showControls = null)
    {
        try
        {
            var video = await _context.VideoHostings
                .Where(v => v.Id == videoId && v.Status == VideoStatus.Ready)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (video == null)
                return null;

            var settings = await _context.VideoHostingSettings
                .Where(s => s.CompanyId == video.CompanyId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // Check if embedding is allowed
            if (settings?.AllowEmbedding == false)
                return null;

            // Check domain restrictions
            if (!string.IsNullOrEmpty(settings?.AllowedDomains) && !string.IsNullOrEmpty(domain))
            {
                var allowedDomains = System.Text.Json.JsonSerializer.Deserialize<string[]>(settings.AllowedDomains);
                if (allowedDomains != null && !allowedDomains.Contains(domain))
                    return null;
            }

            // Check if video is public or has valid access
            if (!video.IsPublic)
                return null;

            // Generate public URL for R2Storage
            if (string.IsNullOrEmpty(video.S3Key))
                return null;

            var videoUrl = _r2StorageService.GetPublicUrl(video.S3Key);

            return new VideoPlayerDto
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                VideoUrl = videoUrl,
                ThumbnailUrl = video.ThumbnailS3Url,
                Width = video.Width,
                Height = video.Height,
                Duration = video.Duration,
                AutoPlay = autoPlay ?? settings?.AutoPlay ?? false,
                Muted = muted ?? false,
                ShowControls = showControls ?? settings?.ShowControls ?? true,
                AllowDownload = settings?.AllowDownload ?? false,
                PlayerTheme = settings?.PlayerTheme ?? "default",
                CustomPlayerCss = settings?.CustomPlayerCss
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting embed player data for video {VideoId}", videoId);
            return null;
        }
    }

    public async Task<VideoPlayerDto?> GetPublicVideoPlayerDataAsync(Guid videoId, string? accessToken = null, bool? autoPlay = null, bool? muted = null, bool? showControls = null)
    {
        try
        {
            var video = await _context.VideoHostings
                .Where(v => v.Id == videoId && v.Status == VideoStatus.Ready)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (video == null)
                return null;

            // Check access permissions
            if (!video.IsPublic && (string.IsNullOrEmpty(accessToken) || video.AccessToken != accessToken))
                return null;

            var settings = await _context.VideoHostingSettings
                .Where(s => s.CompanyId == video.CompanyId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // Generate public URL for R2Storage
            if (string.IsNullOrEmpty(video.S3Key))
                return null;

            var videoUrl = _r2StorageService.GetPublicUrl(video.S3Key);

            return new VideoPlayerDto
            {
                Id = video.Id,
                Title = video.Title,
                Description = video.Description,
                VideoUrl = videoUrl,
                ThumbnailUrl = video.ThumbnailS3Url,
                Width = video.Width,
                Height = video.Height,
                Duration = video.Duration,
                AutoPlay = autoPlay ?? settings?.AutoPlay ?? false,
                Muted = muted ?? false,
                ShowControls = showControls ?? settings?.ShowControls ?? true,
                AllowDownload = settings?.AllowDownload ?? false,
                PlayerTheme = settings?.PlayerTheme ?? "default",
                CustomPlayerCss = settings?.CustomPlayerCss
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public video player data for video {VideoId}", videoId);
            return null;
        }
    }

    public async Task<string?> GetDirectVideoUrlAsync(Guid videoId, string? accessToken = null)
    {
        try
        {
            var video = await _context.VideoHostings
                .Where(v => v.Id == videoId && v.Status == VideoStatus.Ready)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (video == null)
                return null;

            // Check access permissions
            if (!video.IsPublic && (string.IsNullOrEmpty(accessToken) || video.AccessToken != accessToken))
                return null;

            if (string.IsNullOrEmpty(video.S3Key))
                return null;

            return _r2StorageService.GetPublicUrl(video.S3Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting direct video URL for video {VideoId}", videoId);
            return null;
        }
    }

    #endregion

    #region Helper Methods

    private async Task<VideoHostingSettings> GetOrCreateSettingsAsync(Guid companyId, string userId)
    {
        var settings = await _context.VideoHostingSettings
            .FirstOrDefaultAsync(s => s.CompanyId == companyId);

        if (settings == null)
        {
            settings = new VideoHostingSettings
            {
                CompanyId = companyId,
                UpdatedByUserId = userId
            };
            _context.VideoHostingSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return settings;
    }

    private string GetStatusText(VideoStatus status)
    {
        return status switch
        {
            VideoStatus.Uploading => "Yükleniyor",
            VideoStatus.Processing => "İşleniyor",
            VideoStatus.Ready => "Hazır",
            VideoStatus.Failed => "Başarısız",
            VideoStatus.Deleted => "Silindi",
            _ => "Bilinmiyor"
        };
    }

    private string GenerateIframeEmbedCode(Guid videoId, int width, int height, bool autoPlay = false, bool muted = false, bool showControls = true)
    {
        var baseUrl = "https://admin.pushonica.com"; // This should come from configuration
        var queryParams = new List<string>();
        if (autoPlay) queryParams.Add("autoplay=1");
        if (muted) queryParams.Add("muted=1");
        if (!showControls) queryParams.Add("controls=0");
        if (width > 0) queryParams.Add($"w={width}");
        if (height > 0) queryParams.Add($"h={height}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";

        return $"<iframe src=\"{baseUrl}/VideoHosting/Embed/{videoId}{queryString}\" width=\"{width}\" height=\"{height}\" frameborder=\"0\" allow=\"autoplay; fullscreen; picture-in-picture\" allowfullscreen style=\"max-width:100%;border:none;\"></iframe>";
    }

    #endregion

    #region MB Quota (Package-Based)

    public async Task<(bool HasQuota, string Message, decimal RemainingMB)> CheckVideoQuotaAsync(
        Guid companyId, decimal requiredMB)
    {
        try
        {
            var videoModule = await _context.Modules
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Name == "Video Hosting" && m.IsActive);

            if (videoModule == null)
                return (false, "Video Hosting modülü bulunamadı.", 0);

            var quota = await _context.CompanyModuleQuotas
                .Include(q => q.Module)
                .Where(q => q.CompanyId == companyId && q.ModuleId == videoModule.Id)
                .FirstOrDefaultAsync();

            if (quota == null)
                return (false, "Aktif bir Video Hosting paketiniz bulunmuyor. Lütfen bir paket satın alın.", 0);

            // Check if the associated CompanyModule is still within its subscription period
            var companyModule = await _context.CompanyModules
                .Where(cm => cm.CompanyId == companyId && cm.ModuleId == videoModule.Id)
                .FirstOrDefaultAsync();

            if (companyModule?.ExpiresAt.HasValue == true && companyModule.ExpiresAt.Value < DateTime.UtcNow)
                return (false, "Video Hosting paketinizin süresi dolmuştur. Lütfen paketinizi yenileyin.", quota.RemainingQuota);

            if (quota.RemainingQuota < requiredMB)
                return (false, $"Yetersiz depolama kotası. Gerekli: {requiredMB:N0} MB, Kalan: {quota.RemainingQuota:N0} MB", quota.RemainingQuota);

            return (true, "Yeterli kota mevcut.", quota.RemainingQuota);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking video quota for company {CompanyId}", companyId);
            return (false, "Kota kontrolü sırasında hata oluştu.", 0);
        }
    }

    public async Task<bool> DeductVideoQuotaAsync(Guid companyId, decimal usedMB)
    {
        try
        {
            var videoModule = await _context.Modules
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Name == "Video Hosting" && m.IsActive);

            if (videoModule == null) return false;

            var quota = await _context.CompanyModuleQuotas
                .Where(q => q.CompanyId == companyId && q.ModuleId == videoModule.Id)
                .FirstOrDefaultAsync();

            if (quota == null) return false;

            quota.RemainingQuota = Math.Max(0, quota.RemainingQuota - usedMB);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deducted {UsedMB} MB from Video Hosting quota for company {CompanyId}. Remaining: {Remaining} MB",
                usedMB, companyId, quota.RemainingQuota);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting video quota for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<(bool Found, decimal TotalMB, decimal RemainingMB, string PackageName)> GetQuotaInfoAsync(Guid companyId)
    {
        try
        {
            var videoModule = await _context.Modules
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Name == "Video Hosting" && m.IsActive);

            if (videoModule == null)
                return (false, 0, 0, string.Empty);

            var quota = await _context.CompanyModuleQuotas
                .Include(q => q.ModulePackage)
                .Where(q => q.CompanyId == companyId && q.ModuleId == videoModule.Id)
                .FirstOrDefaultAsync();

            if (quota == null)
                return (false, 0, 0, string.Empty);

            return (true, quota.TotalQuota, quota.RemainingQuota, quota.ModulePackage?.Name ?? "Paket");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video quota info for company {CompanyId}", companyId);
            return (false, 0, 0, string.Empty);
        }
    }

    #endregion
}
