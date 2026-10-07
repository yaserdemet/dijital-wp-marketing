using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.DTOs;

namespace PushDashboard.Services.Modules.VideoHosting;

public class VideoHostingModuleService : IVideoHostingModuleService
{
    private readonly ApplicationDbContext _context;
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly ILogger<VideoHostingModuleService> _logger;

    public VideoHostingModuleService(
        ApplicationDbContext context,
        IVideoProcessingService videoProcessingService,
        ILogger<VideoHostingModuleService> logger)
    {
        _context = context;
        _videoProcessingService = videoProcessingService;
        _logger = logger;
    }

    #region Module Status

    public async Task<bool> HasActiveModuleAsync(Guid companyId)
    {
        try
        {
            var videoHostingModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Video Hosting" && m.IsActive);

            if (videoHostingModule == null)
            {
                return false;
            }

            // Sahiplik (IsOwned), aktiflik (IsActive) ve pasif olmama (!IsPassive) kontrolü
            return await _context.CompanyModules
                .AnyAsync(cm => cm.CompanyId == companyId &&
                               cm.ModuleId == videoHostingModule.Id &&
                               cm.IsOwned &&
                               cm.IsActive &&
                               !cm.IsPassive &&
                               (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking active module for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<ModuleStatusDto> GetModuleStatusAsync(Guid companyId)
    {
        try
        {
            var videoHostingModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Video Hosting" && m.IsActive);

            if (videoHostingModule == null)
            {
                return new ModuleStatusDto { IsActive = false };
            }

            // Sahiplik kontrolü için IsOwned kullan
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.PurchasedByUser)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.ModuleId == videoHostingModule.Id && cm.IsOwned);

            if (companyModule == null)
            {
                return new ModuleStatusDto { IsActive = false };
            }

            var hasSettings = await _context.VideoHostingSettings
                .AnyAsync(s => s.CompanyId == companyId);

            var lastUsage = await _context.ModuleUsageLogs
                .Where(mul => mul.CompanyId == companyId && mul.ModuleId == videoHostingModule.Id)
                .OrderByDescending(mul => mul.CreatedAt)
                .FirstOrDefaultAsync();

            return new ModuleStatusDto
            {
                IsActive = companyModule.IsActive && (companyModule.ExpiresAt == null || companyModule.ExpiresAt > DateTime.Now),
                PurchasedAt = companyModule.PurchasedAt,
                ExpiresAt = companyModule.ExpiresAt,
                PaidAmount = companyModule.PaidAmount,
                PurchasedByUserName = companyModule.PurchasedByUser?.UserName ?? "Unknown",
                HasSettings = hasSettings,
                LastUsedAt = lastUsage?.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module status for company {CompanyId}", companyId);
            return new ModuleStatusDto { IsActive = false };
        }
    }

    public async Task<(bool CanUse, string Message, decimal RequiredCredits)> ValidateModuleUsageAsync(Guid companyId, string operationType)
    {
        try
        {
            // Check if module is active
            var hasActiveModule = await HasActiveModuleAsync(companyId);
            if (!hasActiveModule)
            {
                return (false, "Video Hosting modülü aktif değil.", 0);
            }

            // Get company credit balance
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company == null)
            {
                return (false, "Şirket bilgisi bulunamadı.", 0);
            }

            // Calculate required credits based on operation type
            decimal requiredCredits = operationType switch
            {
                "video_upload" => 1.0m, // Base cost, will be calculated based on duration
                "video_view" => 0.0m, // Free
                "video_download" => 0.1m,
                _ => 0.0m
            };

            if (company.CreditBalance < requiredCredits)
            {
                return (false, $"Yetersiz kredi bakiyesi. Gerekli: {requiredCredits:F2}, Mevcut: {company.CreditBalance:F2}", requiredCredits);
            }

            return (true, "Modül kullanımı için uygun.", requiredCredits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating module usage for company {CompanyId}, operation {OperationType}", companyId, operationType);
            return (false, "Modül kullanım doğrulama sırasında hata oluştu.", 0);
        }
    }

    #endregion

    #region Module Settings

    public async Task<VideoSettingsDto?> GetModuleSettingsAsync(Guid companyId)
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
            _logger.LogError(ex, "Error getting module settings for company {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> UpdateModuleSettingsAsync(
        Guid companyId, 
        VideoSettingsRequestDto request, 
        string userId)
    {
        try
        {
            var settings = await _context.VideoHostingSettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (settings == null)
            {
                settings = new Models.VideoHostingSettings
                {
                    CompanyId = companyId,
                    UpdatedByUserId = userId
                };
                _context.VideoHostingSettings.Add(settings);
            }

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

            _logger.LogInformation("Module settings updated for company {CompanyId}", companyId);

            return (true, "Modül ayarları başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module settings for company {CompanyId}", companyId);
            return (false, "Modül ayarları güncelleme sırasında hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> ResetModuleSettingsAsync(Guid companyId, string userId)
    {
        try
        {
            var settings = await _context.VideoHostingSettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (settings == null)
            {
                return (false, "Ayarlar bulunamadı.");
            }

            // Reset to default values
            settings.CostPerMinute = 0.5m;
            settings.StorageCostPerGBPerMonth = 0.1m;
            settings.MaxFileSizeBytes = 500 * 1024 * 1024; // 500MB
            settings.MaxDurationMinutes = 60;
            settings.MaxVideosPerCompany = 100;
            settings.AutoPlay = false;
            settings.ShowControls = true;
            settings.AllowDownload = false;
            settings.PlayerTheme = "default";
            settings.CustomPlayerCss = null;
            settings.RequireAuthentication = false;
            settings.AllowEmbedding = true;
            settings.AllowedDomains = null;
            settings.UpdatedAt = DateTime.Now;
            settings.UpdatedByUserId = userId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Module settings reset to defaults for company {CompanyId}", companyId);

            return (true, "Modül ayarları varsayılan değerlere sıfırlandı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting module settings for company {CompanyId}", companyId);
            return (false, "Modül ayarları sıfırlama sırasında hata oluştu.");
        }
    }

    #endregion

    #region Module Usage

    public async Task<List<ModuleUsageDto>> GetModuleUsageHistoryAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
    {
        try
        {
            var videoHostingModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Video Hosting");

            if (videoHostingModule == null)
                return new List<ModuleUsageDto>();

            var query = _context.ModuleUsageLogs
                .Where(mul => mul.CompanyId == companyId && mul.ModuleId == videoHostingModule.Id);

            if (startDate.HasValue)
                query = query.Where(mul => mul.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(mul => mul.CreatedAt <= endDate.Value);

            var usageLogs = await query
                .OrderByDescending(mul => mul.CreatedAt)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync();

            return usageLogs.Select(mul => new ModuleUsageDto
            {
                Id = mul.Id,
                UsageType = mul.UsageType,
                Description = mul.Description,
                Cost = mul.Cost,
                BalanceBefore = mul.BalanceBefore,
                BalanceAfter = mul.BalanceAfter,
                IsSuccessful = mul.IsSuccessful,
                CreatedAt = mul.CreatedAt,
                ReferenceId = mul.ReferenceId
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module usage history for company {CompanyId}", companyId);
            return new List<ModuleUsageDto>();
        }
    }

    public async Task<ModuleUsageStatsDto> GetModuleUsageStatsAsync(Guid companyId)
    {
        try
        {
            var videoHostingModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Video Hosting");

            if (videoHostingModule == null)
                return new ModuleUsageStatsDto();

            var usageLogs = await _context.ModuleUsageLogs
                .Where(mul => mul.CompanyId == companyId && mul.ModuleId == videoHostingModule.Id)
                .AsNoTracking()
                .ToListAsync();

            if (!usageLogs.Any())
                return new ModuleUsageStatsDto();

            var successfulUsage = usageLogs.Where(mul => mul.IsSuccessful).ToList();

            return new ModuleUsageStatsDto
            {
                TotalUsageCount = usageLogs.Count,
                TotalCost = usageLogs.Sum(mul => mul.Cost),
                AverageCostPerUsage = usageLogs.Any() ? usageLogs.Average(mul => mul.Cost) : 0,
                FirstUsageDate = usageLogs.Min(mul => mul.CreatedAt),
                LastUsageDate = usageLogs.Max(mul => mul.CreatedAt),
                SuccessfulUsageCount = successfulUsage.Count,
                FailedUsageCount = usageLogs.Count - successfulUsage.Count,
                SuccessRate = usageLogs.Count > 0 ? (decimal)successfulUsage.Count / usageLogs.Count * 100 : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module usage stats for company {CompanyId}", companyId);
            return new ModuleUsageStatsDto();
        }
    }

    #endregion

    #region Module Analytics

    public async Task<VideoHostingAnalyticsDto> GetModuleAnalyticsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var analytics = new VideoHostingAnalyticsDto();

            // Get video stats
            analytics.VideoStats = await GetVideoStatsAsync(companyId);

            // Get usage stats
            analytics.UsageStats = await GetModuleUsageStatsAsync(companyId);

            // Get daily usage
            analytics.DailyUsage = await GetDailyUsageAsync(companyId, startDate, endDate);

            // Get top performing videos
            analytics.TopVideos = await GetTopVideosAsync(companyId, 10);

            // Get storage analytics
            analytics.StorageAnalytics = await GetStorageAnalyticsAsync(companyId);

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module analytics for company {CompanyId}", companyId);
            return new VideoHostingAnalyticsDto();
        }
    }

    private async Task<VideoStatsDto> GetVideoStatsAsync(Guid companyId)
    {
        var videos = await _context.VideoHostings
            .Where(v => v.CompanyId == companyId && v.Status != Models.VideoStatus.Deleted)
            .AsNoTracking()
            .ToListAsync();

        var totalViews = await _context.VideoHostingViews
            .Where(vv => vv.CompanyId == companyId)
            .CountAsync();

        return new VideoStatsDto
        {
            TotalVideos = videos.Count,
            ReadyVideos = videos.Count(v => v.Status == Models.VideoStatus.Ready),
            ProcessingVideos = videos.Count(v => v.Status == Models.VideoStatus.Processing || v.Status == Models.VideoStatus.Uploading),
            FailedVideos = videos.Count(v => v.Status == Models.VideoStatus.Failed),
            TotalStorageBytes = videos.Sum(v => v.FileSizeBytes),
            TotalStorageFormatted = _videoProcessingService.FormatFileSize(videos.Sum(v => v.FileSizeBytes)),
            TotalDuration = TimeSpan.FromTicks(videos.Sum(v => v.Duration.Ticks)),
            TotalDurationFormatted = _videoProcessingService.FormatDuration(TimeSpan.FromTicks(videos.Sum(v => v.Duration.Ticks))),
            TotalViews = totalViews,
            TotalUploadCosts = videos.Sum(v => v.UploadCost),
            MonthlyStorageCosts = videos.Sum(v => v.StorageCostPerMonth)
        };
    }

    private async Task<List<DailyUsageDto>> GetDailyUsageAsync(Guid companyId, DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.Now.AddDays(-30);
        var end = endDate ?? DateTime.Now;

        var videos = await _context.VideoHostings
            .Where(v => v.CompanyId == companyId && v.CreatedAt >= start && v.CreatedAt <= end)
            .AsNoTracking()
            .ToListAsync();

        var views = await _context.VideoHostingViews
            .Where(vv => vv.CompanyId == companyId && vv.ViewedAt >= start && vv.ViewedAt <= end)
            .AsNoTracking()
            .ToListAsync();

        var videoHostingModule = await _context.Modules
            .FirstOrDefaultAsync(m => m.Name == "Video Hosting");

        var usageLogs = new List<Models.ModuleUsageLog>();
        if (videoHostingModule != null)
        {
            usageLogs = await _context.ModuleUsageLogs
                .Where(mul => mul.CompanyId == companyId &&
                             mul.ModuleId == videoHostingModule.Id &&
                             mul.CreatedAt >= start &&
                             mul.CreatedAt <= end)
                .AsNoTracking()
                .ToListAsync();
        }

        var dailyUsage = new List<DailyUsageDto>();
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            var dayVideos = videos.Where(v => v.CreatedAt.Date == date).ToList();
            var dayViews = views.Where(vv => vv.ViewedAt.Date == date).ToList();
            var dayUsage = usageLogs.Where(ul => ul.CreatedAt.Date == date).ToList();

            dailyUsage.Add(new DailyUsageDto
            {
                Date = date,
                VideoUploads = dayVideos.Count,
                TotalViews = dayViews.Count,
                TotalCost = dayUsage.Sum(ul => ul.Cost),
                StorageUsed = dayVideos.Sum(v => v.FileSizeBytes)
            });
        }

        return dailyUsage;
    }

    private async Task<List<VideoPerformanceDto>> GetTopVideosAsync(Guid companyId, int limit)
    {
        var videos = await _context.VideoHostings
            .Where(v => v.CompanyId == companyId && v.Status == Models.VideoStatus.Ready)
            .OrderByDescending(v => v.ViewCount)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        return videos.Select(v => new VideoPerformanceDto
        {
            VideoId = v.Id,
            Title = v.Title,
            ViewCount = v.ViewCount,
            Duration = v.Duration,
            CreatedAt = v.CreatedAt,
            EngagementRate = v.Duration.TotalSeconds > 0 ? (decimal)(v.ViewCount / v.Duration.TotalMinutes) : 0
        }).ToList();
    }

    private async Task<StorageAnalyticsDto> GetStorageAnalyticsAsync(Guid companyId)
    {
        var videos = await _context.VideoHostings
            .Where(v => v.CompanyId == companyId && v.Status != Models.VideoStatus.Deleted)
            .AsNoTracking()
            .ToListAsync();

        if (!videos.Any())
        {
            return new StorageAnalyticsDto();
        }

        var largestVideo = videos.OrderByDescending(v => v.FileSizeBytes).First();

        return new StorageAnalyticsDto
        {
            TotalStorageBytes = videos.Sum(v => v.FileSizeBytes),
            TotalStorageFormatted = _videoProcessingService.FormatFileSize(videos.Sum(v => v.FileSizeBytes)),
            MonthlyStorageCost = videos.Sum(v => v.StorageCostPerMonth),
            AverageVideoSize = videos.Any() ? (long)videos.Average(v => v.FileSizeBytes) : 0,
            LargestVideoTitle = largestVideo.Title,
            LargestVideoSize = largestVideo.FileSizeBytes
        };
    }

    #endregion
}
