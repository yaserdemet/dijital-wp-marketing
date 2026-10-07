using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.ViewModels;
using PushDashboard.Models;

namespace PushDashboard.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly ILogger<DashboardService> _logger;


    public DashboardService(
        ApplicationDbContext context,
        IModuleUsageService moduleUsageService,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _moduleUsageService = moduleUsageService;
        _logger = logger;
    }

    public async Task<DashboardIndexViewModel> GetDashboardDataAsync(Guid companyId)
    {
        try
        {
            var stats = await GetDashboardStatsAsync(companyId);
            var moduleUsage = await GetModuleUsageOverviewAsync(companyId);
            var integrationStatus = await GetIntegrationStatusAsync(companyId);
            var recentActivities = await GetRecentActivitiesAsync(companyId);
            var notificationMetrics = await GetNotificationMetricsAsync(companyId);
            var charts = await GetDashboardChartsAsync(companyId);
            var quickActions = await GetQuickActionsAsync(companyId);
            var syncInfo = await GetSyncInfoAsync(companyId);
            
            // Default to last 1 month for initial load
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-1);
            var moduleRevenues = await GetModuleRevenuesAsync(companyId, startDate, endDate);

            return new DashboardIndexViewModel
            {
                Stats = stats,
                ModuleUsage = moduleUsage,
                IntegrationStatus = integrationStatus,
                RecentActivities = recentActivities,
                NotificationMetrics = notificationMetrics,
                Charts = charts,
                QuickActions = quickActions,
                SyncInfo = syncInfo,
                ModuleRevenues = moduleRevenues
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data for company {CompanyId}", companyId);
            return new DashboardIndexViewModel();
        }
    }

    public async Task<DashboardStatsViewModel> GetDashboardStatsAsync(Guid companyId)
    {
        var today = DateTime.Now.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var company = await _context.Companies.FindAsync(companyId);
        var activeModules = await _context.CompanyModules
            .Where(cm => cm.CompanyId == companyId && cm.IsActive)
            .CountAsync();
        var passiveModules = await _context.CompanyModules
            .Where(cm => cm.CompanyId == companyId && cm.IsPassive)
            .CountAsync();

        var customerStats = await _context.Customers
            .Where(c => c.CompanyId == companyId)
            .GroupBy(c => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(c => c.IsActive),
                Passive = g.Count(c => !c.IsActive),
                New = g.Count(c => c.MembershipDate >= startOfMonth)
            })
            .FirstOrDefaultAsync();

        // UsageType'ları: "Whatsapp Bildirimi", "Sms Bildirimi", "Email Gönderimi", "Toplu Mesaj", vb.
        // Gönderim sayılmayan işlem türlerini hariç tutuyoruz
        var excludedUsageTypes = new[] { "Modül Satın Alma", "Abonelik Yenilendi" };

        var usageStats = await _context.ModuleUsageLogs
            .Where(mul => mul.CompanyId == companyId && mul.CreatedAt >= startOfMonth)
            .GroupBy(mul => 1)
            .Select(g => new
            {
                // Cost alanı gerçek alıcı sayısını yansıtır (toplu gönderimler tek satır ama Cost=alıcı sayısı)
                NotificationsToday = (int)g.Where(mul => mul.CreatedAt >= today && !excludedUsageTypes.Contains(mul.UsageType)).Sum(mul => mul.Cost),
                NotificationsThisMonth = (int)g.Where(mul => !excludedUsageTypes.Contains(mul.UsageType)).Sum(mul => mul.Cost),
                MonthlySpending = g.Sum(mul => mul.Cost)
            })
            .FirstOrDefaultAsync();

        var activeIntegrations = await _context.CompanyIntegrations
            .Where(ci => ci.CompanyId == companyId && ci.IsActive && ci.IsConfigured)
            .CountAsync();
        // Teslimat oranı: (başarılı gönderim / toplam gönderim) × 100
        var deliveryStats = await _context.ModuleUsageLogs
            .Where(mul => mul.CompanyId == companyId &&
                         mul.CreatedAt >= startOfMonth &&
                         !excludedUsageTypes.Contains(mul.UsageType))
            .GroupBy(mul => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Successful = g.Count(mul => mul.IsSuccessful)
            })
            .FirstOrDefaultAsync();

        double averageDeliveryRate = 0;
        if (deliveryStats != null && deliveryStats.Total > 0)
        {
            averageDeliveryRate = Math.Round((double)deliveryStats.Successful / deliveryStats.Total * 100, 1);
        }

        var lastActivityUtc = await _context.ActivityLogs
            .Where(al => al.CompanyId == companyId)
            .OrderByDescending(al => al.CreatedAt)
            .Select(al => al.CreatedAt)
            .FirstOrDefaultAsync();

        var lastActivity = lastActivityUtc;

        return new DashboardStatsViewModel
        {
            ActiveModules = activeModules,
            PassiveModules = passiveModules,
            CreditBalance = company?.CreditBalance ?? 0,
            TotalCustomers = customerStats?.Total ?? 0,
            ActiveCustomers = customerStats?.Active ?? 0,
            PassiveCustomers = customerStats?.Passive ?? 0,
            NewCustomers = customerStats?.New ?? 0,
            NotificationsSentToday = usageStats?.NotificationsToday ?? 0,
            NotificationsSentThisMonth = usageStats?.NotificationsThisMonth ?? 0,
            MonthlySpending = usageStats?.MonthlySpending ?? 0,
            ActiveIntegrations = activeIntegrations,
            AverageDeliveryRate = averageDeliveryRate,
            LastActivity = lastActivity
        };
    }

    public async Task<List<ModuleUsageOverviewViewModel>> GetModuleUsageOverviewAsync(Guid companyId)
    {
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        var companyModules = await _context.CompanyModules
            .Include(cm => cm.Module)
            .Where(cm => cm.CompanyId == companyId)
            .ToListAsync();

        var moduleUsageStats = await _context.ModuleUsageLogs
            .Where(mul => mul.CompanyId == companyId && mul.CreatedAt >= startOfMonth)
            .GroupBy(mul => mul.ModuleId)
            .Select(g => new
            {
                ModuleId = g.Key,
                UsageCount = g.Count(),
                TotalCost = g.Sum(mul => mul.Cost),
                LastUsed = g.Max(mul => mul.CreatedAt)
            })
            .ToListAsync();

        var result = new List<ModuleUsageOverviewViewModel>();

        foreach (var companyModule in companyModules)
        {
            var stats = moduleUsageStats.FirstOrDefault(s => s.ModuleId == companyModule.ModuleId);
            var successRate = await CalculateModuleSuccessRateAsync(companyModule.Module.Name, companyId, companyModule.IsActive);

            result.Add(new ModuleUsageOverviewViewModel
            {
                ModuleId = companyModule.ModuleId,
                ModuleName = companyModule.Module.Name,
                ModuleIcon = companyModule.Module.IconClass,
                ModuleColor = companyModule.Module.BackgroundColor,
                IsActive = companyModule.IsActive,
                LastUsed = stats?.LastUsed,
                UsageCountThisMonth = stats?.UsageCount ?? 0,
                CostThisMonth = stats?.TotalCost ?? 0,
                SuccessRate = successRate
            });
        }

        return result.OrderByDescending(m => m.IsActive).ThenBy(m => m.ModuleName).ToList();
    }

    public async Task<List<IntegrationStatusViewModel>> GetIntegrationStatusAsync(Guid companyId)
    {
        var companyIntegrations = await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .Where(ci => ci.CompanyId == companyId)
            .ToListAsync();

        var result = new List<IntegrationStatusViewModel>();

        foreach (var integration in companyIntegrations)
        {
            // Get recent sync logs for this integration
            var recentSync = await _context.SyncLogs
                .Where(sl => sl.CompanyId == companyId &&
                           sl.SyncType == integration.Integration.Name)
                .OrderByDescending(sl => sl.SyncStartTime)
                .FirstOrDefaultAsync();

            var today = DateTime.Now.Date;
            var todayLogs = await _context.SyncLogs
                .Where(sl => sl.CompanyId == companyId &&
                           sl.SyncType == integration.Integration.Name &&
                           sl.SyncStartTime.Date == today)
                .ToListAsync();

            // Entegrasyonun bağlı olup olmadığını kontrol et
            // E-ticaret entegrasyonları için apiUrl ve apiKey dolu olmalı
            // Bildirim entegrasyonları için ilgili alanlar dolu olmalı
            var isConnected = CheckIntegrationConnection(integration);

            // Son senkronizasyon zamanını belirle (SyncLog veya LastSyncAt)
            var lastSyncTime = recentSync?.SyncEndTime ?? integration.LastSyncAt;

            result.Add(new IntegrationStatusViewModel
            {
                IntegrationId = integration.IntegrationId,
                IntegrationName = integration.Integration.Name,
                IntegrationType = integration.Integration.Category,
                IsConnected = isConnected,
                IsActive = integration.IsActive,
                LastSync = lastSyncTime,
                LastError = recentSync?.IsSuccessful == false ? recentSync.ErrorMessage : null,
                SuccessfulOperationsToday = todayLogs.Count(l => l.IsSuccessful),
                FailedOperationsToday = todayLogs.Count(l => !l.IsSuccessful),
                DeactivatedByService = integration.DeactivatedByService,
                DeactivationReason = integration.DeactivationReason,
                DeactivatedAt = integration.DeactivatedAt
            });
        }

        // Sadece bağlı (yapılandırılmış) entegrasyonları göster
        return result
            .Where(i => i.IsConnected)
            .OrderByDescending(i => i.IsActive)
            .ThenBy(i => i.IntegrationName)
            .ToList();
    }

    /// <summary>
    /// Entegrasyonun bağlantı durumunu kontrol eder
    /// Sadece gerçekten yapılandırılmış (zorunlu alanları dolu) entegrasyonlar için true döner
    /// </summary>
    private bool CheckIntegrationConnection(CompanyIntegration integration)
    {
        var settings = integration.Settings;
        if (settings == null || !settings.Any())
            return false;

        var integrationName = integration.Integration?.Name?.ToLower() ?? "";
        var category = integration.Integration?.Category ?? "";

        // E-ticaret entegrasyonları (Ticimax, IKAS, Ideasoft, Tsoft, Shopify)
        if (category == "E-Ticaret" || category == "E-ticaret Altyapıları")
        {
            var apiUrl = settings.GetValueOrDefault("apiUrl")?.ToString()?.Trim();
            var apiKey = settings.GetValueOrDefault("apiKey")?.ToString()?.Trim();

            // Her iki alan da dolu olmalı (boş string değil)
            return !string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey);
        }

        // WhatsApp entegrasyonu
        if (integrationName.Contains("whatsapp"))
        {
            var accessToken = settings.GetValueOrDefault("accessToken")?.ToString()?.Trim();
            var phoneNumberId = settings.GetValueOrDefault("phoneNumberId")?.ToString()?.Trim();

            return !string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(phoneNumberId);
        }

        // Email SMTP entegrasyonu
        if (integrationName.Contains("email") || integrationName.Contains("smtp"))
        {
            var smtpServer = settings.GetValueOrDefault("smtpServer")?.ToString()?.Trim();
            var username = settings.GetValueOrDefault("username")?.ToString()?.Trim();

            return !string.IsNullOrWhiteSpace(smtpServer) && !string.IsNullOrWhiteSpace(username);
        }

        // Telegram entegrasyonu
        if (integrationName.Contains("telegram"))
        {
            var botToken = settings.GetValueOrDefault("botToken")?.ToString()?.Trim();

            return !string.IsNullOrWhiteSpace(botToken);
        }

        // Diğer entegrasyonlar için - en az bir anlamlı değer olmalı
        return settings.Any(s =>
        {
            var value = s.Value?.ToString()?.Trim();
            // Boş string, "0", "false" gibi varsayılan değerleri sayma
            return !string.IsNullOrWhiteSpace(value) &&
                   value != "0" &&
                   value.ToLower() != "false";
        });
    }

    public async Task<List<RecentActivityViewModel>> GetRecentActivitiesAsync(Guid companyId, int limit = 10)
    {
        var recentLogs = await _context.ModuleUsageLogs
            .Include(mul => mul.Module)
            .Where(mul => mul.CompanyId == companyId)
            .OrderByDescending(mul => mul.CreatedAt)
            .Take(limit)
            .ToListAsync();

        var activities = new List<RecentActivityViewModel>();

        foreach (var log in recentLogs)
        {
            var activityType = DetermineActivityType(log.UsageType);
            var description = GenerateActivityDescription(log);

            activities.Add(new RecentActivityViewModel
            {
                ActivityType = activityType,
                Description = description,
                Timestamp = log.CreatedAt,
                ModuleName = log.Module?.Name,
                Status = "completed", // Could be derived from log data
                Cost = log.Cost > 0 ? log.Cost : null
            });
        }

        return activities;
    }

    public async Task<List<NotificationMetricsViewModel>> GetNotificationMetricsAsync(Guid companyId)
    {
        var today = DateTime.Now.Date;
        var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        var startOfWeek = today.AddDays(-1 * diff).Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var logs = await _context.ModuleUsageLogs
            .Where(m => m.CompanyId == companyId && m.CreatedAt >= startOfMonth)
            .Select(m => new
            {
                m.Channel,
                m.UsageType,
                m.CreatedAt,
                m.IsSuccessful,
                m.Cost
            })
            .ToListAsync();

        var rawGrouped = logs
            .Where(l => (!string.IsNullOrEmpty(l.Channel) || !string.IsNullOrEmpty(l.UsageType)))
            .GroupBy(l => !string.IsNullOrEmpty(l.Channel) ? l.Channel.Trim() : l.UsageType?.Trim() ?? "")
            .ToList();

        var normalized = rawGrouped
            .Select(g => new
            {
                GroupKey = NotificationMetricsGroupKey(g.Key),
                TurkishLabel = NotificationMetricsTurkishLabel(g.Key),
                Items = g.ToList()
            })
            .GroupBy(x => x.GroupKey)
            .Select(g => new
            {
                GroupKey = g.Key,
                TurkishLabel = g.First().TurkishLabel,
                Items = g.SelectMany(x => x.Items).ToList()
            })
            .Where(x => !string.IsNullOrEmpty(x.GroupKey))
            .OrderByDescending(x => x.Items.Count)
            .ToList();

        var metrics = new List<NotificationMetricsViewModel>();

        foreach (var group in normalized)
        {
            var items = group.Items;
            // Cost alanı gerçek alıcı sayısını yansıtır (toplu gönderimler tek satır ama Cost=alıcı sayısı)
            var sentToday = (int)items.Where(x => x.CreatedAt.Date == today).Sum(x => x.Cost);
            var sentThisWeek = (int)items.Where(x => x.CreatedAt.Date >= startOfWeek).Sum(x => x.Cost);
            var sentThisMonth = (int)items.Sum(x => x.Cost);
            var deliveredToday = (int)items.Where(x => x.CreatedAt.Date == today && x.IsSuccessful).Sum(x => x.Cost);
            var failedToday = (int)items.Where(x => x.CreatedAt.Date == today && !x.IsSuccessful).Sum(x => x.Cost);
            var costToday = items.Where(x => x.CreatedAt.Date == today).Sum(x => x.Cost);
            var costThisMonth = items.Sum(x => x.Cost);

            metrics.Add(new NotificationMetricsViewModel
            {
                ChannelType = group.GroupKey,
                ChannelName = group.TurkishLabel,
                SentToday = sentToday,
                SentThisWeek = sentThisWeek,
                SentThisMonth = sentThisMonth,
                DeliveredToday = deliveredToday,
                FailedToday = failedToday,
                CostToday = costToday,
                CostThisMonth = costThisMonth
            });
        }

        return metrics;
    }

    private static string NotificationMetricsGroupKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var n = raw.Trim();
        if (n.Equals("Gift_wheel", StringComparison.OrdinalIgnoreCase) || (n.Contains("Gift", StringComparison.OrdinalIgnoreCase) && n.Contains("wheel", StringComparison.OrdinalIgnoreCase))) return "hediye_çarkı";
        if (n.IndexOf("email", StringComparison.OrdinalIgnoreCase) >= 0 && n.IndexOf("sms", StringComparison.OrdinalIgnoreCase) >= 0 && n.IndexOf("whatsapp", StringComparison.OrdinalIgnoreCase) >= 0) return "çoklu_kanal";
        if (n.Contains("Video", StringComparison.OrdinalIgnoreCase) || n.Equals("Video_upload", StringComparison.OrdinalIgnoreCase)) return "video_yükleme";
        if (n.Equals("Trendyol", StringComparison.OrdinalIgnoreCase)) return "trendyol";
        if (n.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase) || n.Equals("Whatsapp", StringComparison.OrdinalIgnoreCase)) return "whatsapp";
        if (n.Equals("Email", StringComparison.OrdinalIgnoreCase) || n.Equals("E-posta", StringComparison.OrdinalIgnoreCase)) return "email";
        if (n.Equals("Sms", StringComparison.OrdinalIgnoreCase) || n.Equals("SMS", StringComparison.OrdinalIgnoreCase)) return "sms";
        if (n.Equals("Bulk_message", StringComparison.OrdinalIgnoreCase)) return "toplu_mesaj";
        if (n.Contains("Yorum", StringComparison.OrdinalIgnoreCase) && (n.Contains("taşıma", StringComparison.OrdinalIgnoreCase) || n.Contains("tasima", StringComparison.OrdinalIgnoreCase))) return "yorum_taşıma";
        if (n.Contains("Hediye çeki", StringComparison.OrdinalIgnoreCase) || n.Equals("Voucher_creation", StringComparison.OrdinalIgnoreCase)) return "hediye_ceki";
        return n.ToLowerInvariant().Replace(" ", "_");
    }

    private static string NotificationMetricsTurkishLabel(string? raw)
    {
        var key = NotificationMetricsGroupKey(raw);
        return key switch
        {
            "hediye_çarkı" => "Hediye Çarkı",
            "whatsapp" => "WhatsApp",
            "email" => "E-posta",
            "sms" => "SMS",
            "trendyol" => "Trendyol",
            "çoklu_kanal" => "E-posta, SMS, WhatsApp",
            "video_yükleme" => "Video Yükleme",
            "toplu_mesaj" => "Toplu Mesaj",
            "yorum_taşıma" => "Yorum Taşıma",
            "hediye_ceki" => "Hediye Çeki Oluşturma",
            _ => string.IsNullOrEmpty(key) ? "Diğer" : (key.Length > 0 ? char.ToUpper(key[0]) + key.Substring(1).Replace("_", " ") : key)
        };
    }

    public async Task<DashboardChartsViewModel> GetDashboardChartsAsync(Guid companyId)
    {
        return await GetDashboardChartsAsync(companyId, 7);
    }

    public async Task<DashboardChartsViewModel> GetDashboardChartsAsync(Guid companyId, int days)
    {
        // Geçerli gün sayısı kontrolü
        if (days <= 0) days = 7;
        if (days > 365) days = 365;

        var dateRange = Enumerable.Range(0, days)
            .Select(i => DateTime.Now.Date.AddDays(-i))
            .Reverse()
            .ToList();

        var usageTrend = new List<ChartDataPoint>();
        var costTrend = new List<ChartDataPoint>();

        // 30 gün için tarih formatını farklı yap
        var dateFormat = days <= 7 ? "dd MMM" : "dd/MM";

        foreach (var date in dateRange)
        {
            var dayUsage = await _context.ModuleUsageLogs
                .Where(mul => mul.CompanyId == companyId && mul.CreatedAt.Date == date)
                .CountAsync();

            var dayCost = await _context.ModuleUsageLogs
                .Where(mul => mul.CompanyId == companyId && mul.CreatedAt.Date == date)
                .SumAsync(mul => mul.Cost);

            usageTrend.Add(new ChartDataPoint
            {
                Label = date.ToString(dateFormat),
                Value = dayUsage,
                Date = date
            });

            costTrend.Add(new ChartDataPoint
            {
                Label = date.ToString(dateFormat),
                Value = dayCost,
                Date = date
            });
        }

        // Channel Distribution (Last 30 days) – gruplu ve Türkçe etiketler
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        var channelStatsRaw = await _context.ModuleUsageLogs
            .Where(mul => mul.CompanyId == companyId && mul.CreatedAt >= thirtyDaysAgo)
            .GroupBy(mul => mul.UsageType)
            .Select(g => new { Channel = g.Key, Count = g.Count() })
            .ToListAsync();

        var grouped = channelStatsRaw
            .Select(x => new { Key = ChannelGroupKey(x.Channel), Label = ChannelTurkishLabel(x.Channel), Count = x.Count })
            .GroupBy(x => x.Key)
            .Select(g => new { GroupKey = g.Key, TurkishLabel = g.First().Label, Count = g.Sum(x => x.Count) })
            .OrderByDescending(x => x.Count)
            .ToList();

        var totalUsage = grouped.Sum(x => x.Count);
        var channelDistribution = new List<PieChartDataPoint>();

        var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "sms", "rgba(168, 85, 247, 0.85)" },
            { "email", "rgba(59, 130, 246, 0.85)" },
            { "whatsapp_bildirim", "rgba(34, 197, 94, 0.85)" },
            { "toplu_mesaj", "rgba(245, 158, 11, 0.85)" },
            { "yorum_tasima", "rgba(236, 72, 153, 0.85)" },
            { "video_yukleme", "rgba(20, 184, 166, 0.85)" },
            { "hediye_ceki", "rgba(239, 68, 68, 0.85)" },
            { "diger", "rgba(107, 114, 128, 0.85)" }
        };

        foreach (var stat in grouped)
        {
            var colorKey = colors.ContainsKey(stat.GroupKey) ? stat.GroupKey : "diger";
            channelDistribution.Add(new PieChartDataPoint
            {
                Label = stat.TurkishLabel,
                Value = stat.Count,
                Percentage = totalUsage > 0 ? Math.Round((double)stat.Count / totalUsage * 100, 1) : 0,
                Color = colors[colorKey]
            });
        }

        return new DashboardChartsViewModel
        {
            UsageTrend = usageTrend,
            CostTrend = costTrend,
            DeliveryRates = new List<ChartDataPoint>(),
            ModuleDistribution = new List<PieChartDataPoint>(),
            ChannelDistribution = channelDistribution
        };
    }

    public async Task<List<QuickActionViewModel>> GetQuickActionsAsync(Guid companyId)
    {
        var companyModules = await _context.CompanyModules
            .Include(cm => cm.Module)
            .Where(cm => cm.CompanyId == companyId && cm.IsActive)
            .ToListAsync();

        var actions = new List<QuickActionViewModel>();

        // Birthday notifications
        if (companyModules.Any(cm => cm.Module.Name == "Doğum Günü Hatırlatıcısı"))
        {
            actions.Add(new QuickActionViewModel
            {
                Title = "Doğum Günü Bildirimleri",
                Description = "Bugün doğum günü olan müşterilere kutlama mesajı gönderin",
                Icon = "gift",
                Color = "pink",
                ActionUrl = "/Birthday/SendNotifications",
                ActionType = "button"
            });
        }

        // Store access
        actions.Add(new QuickActionViewModel
        {
            Title = "Mağaza",
            Description = "Yeni modüller keşfedin ve satın alın",
            Icon = "shopping-bag",
            Color = "blue",
            ActionUrl = "/Store",
            ActionType = "link"
        });

        // Usage history
        actions.Add(new QuickActionViewModel
        {
            Title = "Harcama Geçmişi",
            Description = "Modül kullanım maliyetlerinizi görüntüleyin",
            Icon = "bar-chart",
            Color = "purple",
            ActionUrl = "/CostHistory",
            ActionType = "link"
        });

        // Settings
        actions.Add(new QuickActionViewModel
        {
            Title = "Ayarlar",
            Description = "Hesap ve entegrasyon ayarlarınızı yönetin",
            Icon = "settings",
            Color = "gray",
            ActionUrl = "/Settings",
            ActionType = "link"
        });

        return actions;
    }

    public async Task<object> GetRealTimeDataAsync(Guid companyId)
    {
        var stats = await GetDashboardStatsAsync(companyId);
        var integrationStatus = await GetIntegrationStatusAsync(companyId);

        return new
        {
            creditBalance = stats.FormattedCreditBalance,
            notificationsToday = stats.NotificationsSentToday,
            activeModules = stats.ActiveModules,
            passiveModules = stats.PassiveModules,
            lastActivity = stats.FormattedLastActivity,
            integrationSuccessToday = integrationStatus.Sum(i => i.SuccessfulOperationsToday),
            integrationFailedToday = integrationStatus.Sum(i => i.FailedOperationsToday)
        };
    }

    private string DetermineActivityType(string usageType)
    {
        if (usageType.Contains("notification")) return "notification";
        if (usageType.Contains("sync")) return "sync";
        if (usageType.Contains("purchase")) return "module_purchase";
        if (usageType.Contains("integration")) return "integration";
        return "activity";
    }

    private string GenerateActivityDescription(ModuleUsageLog log)
    {
        var moduleName = log.Module?.Name ?? "Bilinmeyen Modül";

        return log.UsageType switch
        {
            var type when type.Contains("notification") => $"{moduleName} - Bildirim gönderildi",
            var type when type.Contains("sync") => $"{moduleName} - Veri senkronizasyonu",
            var type when type.Contains("purchase") => $"{moduleName} - Modül satın alındı",
            _ => $"{moduleName} - {log.Description}"
        };
    }

    /// <summary>
    /// Her modül için tanımlı formüle göre başarı oranını hesaplar.
    /// </summary>
    private async Task<double> CalculateModuleSuccessRateAsync(string moduleName, Guid companyId, bool isActive)
    {
        try
        {
            switch (moduleName)
            {
                case "Hediye Çarkı":
                {
                    // Başarı (%) = (Kullanılan Kupon / Oluşan Kupon) × 100
                    var giftWheelIds = await _context.GiftWheels
                        .Where(gw => gw.CompanyId == companyId)
                        .Select(gw => gw.Id)
                        .ToListAsync();
                    if (!giftWheelIds.Any()) return 0;

                    var totalVouchersCreated = await _context.GiftWheelSpins
                        .CountAsync(s => giftWheelIds.Contains(s.GiftWheelId) && s.VoucherCreated);
                    var totalVouchersUsed = await _context.GiftWheelSpins
                        .CountAsync(s => giftWheelIds.Contains(s.GiftWheelId) && s.VoucherUsed);

                    return totalVouchersCreated > 0
                        ? Math.Round((double)totalVouchersUsed / totalVouchersCreated * 100, 1)
                        : 0;
                }

                case "Toplu Mesaj Gönderimi":
                {
                    // Başarı (%) = (Başarıyla Gönderilen Mesaj / Toplam Gönderim Denemesi) × 100
                    var bulkStats = await _context.BulkMessages
                        .Where(bm => bm.CompanyId == companyId)
                        .GroupBy(bm => 1)
                        .Select(g => new
                        {
                            TotalProcessed = g.Sum(bm => bm.ProcessedRecipients),
                            TotalSuccessful = g.Sum(bm => bm.SuccessfulSends)
                        })
                        .FirstOrDefaultAsync();

                    return bulkStats != null && bulkStats.TotalProcessed > 0
                        ? Math.Round((double)bulkStats.TotalSuccessful / bulkStats.TotalProcessed * 100, 1)
                        : 0;
                }

                case "Yorum Taşıma":
                {
                    // Başarı (%) = (Barkodla Eşleşen Yorum / Toplam Çekilen Yorum) × 100
                    var moduleId = await _context.Modules
                        .Where(m => m.Name == "Yorum Taşıma")
                        .Select(m => m.Id)
                        .FirstOrDefaultAsync();
                    if (moduleId == 0) return 0;

                    var totalLogs = await _context.ModuleUsageLogs
                        .CountAsync(l => l.CompanyId == companyId && l.ModuleId == moduleId);
                    var successLogs = await _context.ModuleUsageLogs
                        .CountAsync(l => l.CompanyId == companyId && l.ModuleId == moduleId && l.IsSuccessful);

                    return totalLogs > 0
                        ? Math.Round((double)successLogs / totalLogs * 100, 1)
                        : 0;
                }

                case "Favori Ürünler":
                case "Stok Hatırlatma":
                case "İlk Alışveriş Mesajı":
                {
                    // Başarı (%) = (Toplam Başarılı Gönderim / Toplam Gönderim Denemesi) × 100
                    var modId = await _context.Modules
                        .Where(m => m.Name == moduleName)
                        .Select(m => m.Id)
                        .FirstOrDefaultAsync();
                    if (modId == 0) return 0;

                    var total = await _context.ModuleUsageLogs
                        .CountAsync(l => l.CompanyId == companyId && l.ModuleId == modId);
                    var successful = await _context.ModuleUsageLogs
                        .CountAsync(l => l.CompanyId == companyId && l.ModuleId == modId && l.IsSuccessful);

                    return total > 0
                        ? Math.Round((double)successful / total * 100, 1)
                        : 0;
                }

                case "Sipariş Durumu Bildirimleri":
                {
                    // Başarı (%) = (Toplam Başarılı Gönderim / Toplam Gönderim Denemesi) × 100
                    var totalChanges = await _context.OrderStatusChangeLogs
                        .CountAsync(l => l.CompanyId == companyId);
                    var sentNotifications = await _context.OrderStatusChangeLogs
                        .CountAsync(l => l.CompanyId == companyId && l.NotificationSent);

                    return totalChanges > 0
                        ? Math.Round((double)sentNotifications / totalChanges * 100, 1)
                        : 0;
                }

                case "Video Hosting":
                {
                    // Başarı = (Toplam İzlenme Süresi / Toplam Görüntüleme) × 100 (dakika bazlı)
                    var videoIds = await _context.VideoHostings
                        .Where(v => v.CompanyId == companyId)
                        .Select(v => v.Id)
                        .ToListAsync();
                    if (!videoIds.Any()) return 0;

                    var views = await _context.VideoHostingViews
                        .Where(v => videoIds.Contains(v.VideoHostingId))
                        .ToListAsync();
                    if (!views.Any()) return 0;

                    var totalWatchMinutes = views
                        .Where(v => v.WatchDuration.HasValue)
                        .Sum(v => v.WatchDuration!.Value.TotalMinutes);
                    var totalViews = views.Count;

                    return totalViews > 0
                        ? Math.Round(totalWatchMinutes / totalViews * 100, 1)
                        : 0;
                }

                case "Çerez Yönetimi":
                {
                    // Başarı (%) = (Kabul Oranı / Görüntülenme) × 100
                    var cookieManagement = await _context.CookieManagements
                        .Where(cm => cm.CompanyId == companyId)
                        .FirstOrDefaultAsync();
                    if (cookieManagement == null) return 0;

                    var impressions = await _context.CookieConsentEvents
                        .CountAsync(e => e.CookieManagementId == cookieManagement.Id && e.EventType == "impression");
                    var accepts = await _context.CookieConsentEvents
                        .CountAsync(e => e.CookieManagementId == cookieManagement.Id && e.EventType == "accept_all");

                    return impressions > 0
                        ? Math.Round((double)accepts / impressions * 100, 1)
                        : 0;
                }

                case "Çıkış Anketi":
                {
                    // Başarı (%) = (Tamamlanma Sayısı / Görüntülenme) × 100
                    var surveyIds = await _context.ExitSurveys
                        .Where(es => es.CompanyId == companyId)
                        .Select(es => es.Id)
                        .ToListAsync();
                    if (!surveyIds.Any()) return 0;

                    var totalImpressions = await _context.ExitSurveyEvents
                        .CountAsync(e => surveyIds.Contains(e.ExitSurveyId) && e.EventType == "impression");
                    var totalCompleted = await _context.ExitSurveyEvents
                        .CountAsync(e => surveyIds.Contains(e.ExitSurveyId) && e.EventType == "completed");

                    return totalImpressions > 0
                        ? Math.Round((double)totalCompleted / totalImpressions * 100, 1)
                        : 0;
                }

                case "Doğum Günü Hatırlatma":
                {
                    // Başarı (%) = (Kullanılan Hediye Çeki Sayısı / Gönderilen Mesaj) × 100
                    var totalSent = await _context.BirthdayNotifications
                        .CountAsync(bn => bn.CompanyId == companyId);
                    var totalSuccessful = await _context.BirthdayNotifications
                        .CountAsync(bn => bn.CompanyId == companyId && bn.Success);

                    return totalSent > 0
                        ? Math.Round((double)totalSuccessful / totalSent * 100, 1)
                        : 0;
                }

                case "Sepet Hatırlatma":
                {
                    // Başarı (%) = (Toplam Başarılı Gönderim / Toplam Gönderim Denemesi) × 100
                    var totalReminders = await _context.BasketReminderLogs
                        .CountAsync(br => br.CompanyId == companyId);
                    var successfulReminders = await _context.BasketReminderLogs
                        .CountAsync(br => br.CompanyId == companyId && br.IsSuccessful);

                    return totalReminders > 0
                        ? Math.Round((double)successfulReminders / totalReminders * 100, 1)
                        : 0;
                }

                case "Social Proof":
                case "Ürün Slider":
                    // Modül aktifse %100, pasifse %0
                    return isActive ? 100 : 0;

                default:
                {
                    // Bilinmeyen modüller için ModuleUsageLogs'tan genel hesaplama
                    var defaultModId = await _context.Modules
                        .Where(m => m.Name == moduleName)
                        .Select(m => m.Id)
                        .FirstOrDefaultAsync();
                    if (defaultModId == 0) return 0;

                    var totalDefault = await _context.ModuleUsageLogs
                        .CountAsync(l => l.CompanyId == companyId && l.ModuleId == defaultModId);
                    var successDefault = await _context.ModuleUsageLogs
                        .CountAsync(l => l.CompanyId == companyId && l.ModuleId == defaultModId && l.IsSuccessful);

                    return totalDefault > 0
                        ? Math.Round((double)successDefault / totalDefault * 100, 1)
                        : 0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating success rate for module {ModuleName} company {CompanyId}", moduleName, companyId);
            return 0;
        }
    }

    public async Task<SyncInfoViewModel> GetSyncInfoAsync(Guid companyId)
    {
        try
        {
            // Son senkronizasyon zamanını al (CompanyIntegrations tablosundan)
            var lastSync = await _context.CompanyIntegrations
                .Where(ci => ci.CompanyId == companyId && ci.LastSyncAt.HasValue)
                .OrderByDescending(ci => ci.LastSyncAt)
                .Select(ci => ci.LastSyncAt)
                .FirstOrDefaultAsync();

            // Son güncelleme zamanını al (Company tablosundan)
            var lastUpdate = await _context.Companies
                .Where(c => c.Id == companyId)
                .Select(c => c.UpdatedAt)
                .FirstOrDefaultAsync();

            return new SyncInfoViewModel
            {
                LastSyncAt = lastSync,
                LastUpdateAt = lastUpdate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync info for company {CompanyId}", companyId);
            return new SyncInfoViewModel();
        }
    }

    public async Task<List<ModuleFinancialStatsViewModel>> GetModuleRevenuesAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.MinValue;
            // If endDate is provided, utilize the end of that day (23:59:59)
            var end = endDate.HasValue ? endDate.Value.Date.AddDays(1).AddTicks(-1) : DateTime.MaxValue;

            // 1. Get Modules to link IDs and Names
            var modules = await _context.Modules.ToListAsync();

            // 2. Get Expenditures (Cost) from ModuleUsageLog
            // Filter by UsageType "whatsapp_notification" for now as per requirement
            var usageLogs = await _context.ModuleUsageLogs
                .Where(m => m.CompanyId == companyId && 
                            (m.UsageType == "whatsapp_notification" || m.UsageType == "whatsapp" || m.Channel.Contains("whatsapp")) &&
                            m.CreatedAt >= start && m.CreatedAt <= end)
                .GroupBy(m => m.ModuleId)
                .Select(g => new { ModuleId = g.Key, Cost = g.Sum(m => m.Cost) })
                .ToListAsync();

            // 3. Get Revenues from OrderConversions
            var templates = await _context.WhatsAppTemplateSettings
                .Where(t => t.CompanyId == companyId && t.TrackingGuid != null)
                .Select(t => new { t.TemplateName, t.TrackingGuid, t.ModuleType })
                .ToListAsync();

            var trackingGuids = templates.Select(t => t.TrackingGuid.ToString()).ToList();
            
            var conversions = await _context.OrderConversions
                .Where(oc => oc.CompanyId == companyId && 
                             oc.ConversionSource != null && 
                             trackingGuids.Contains(oc.ConversionSource) &&
                             oc.CreatedAt >= start && oc.CreatedAt <= end)
                .GroupBy(oc => oc.ConversionSource)
                .Select(g => new 
                { 
                    Source = g.Key, 
                    Revenue = g.Sum(oc => oc.TotalAmount),
                    Count = g.Count() 
                })
                .ToListAsync();

            // Group Templates by Module Name
            var templatesByModule = templates
                .GroupBy(t => 
                {
                    if (string.IsNullOrEmpty(t.ModuleType)) return "Diğer";
                    return TemplateVariableHelper.GetModuleNameByKey(t.ModuleType);
                })
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<ModuleFinancialStatsViewModel>();

            // 4. Merge Data based on Modules
            foreach (var module in modules)
            {
                var cost = usageLogs.FirstOrDefault(u => u.ModuleId == module.Id)?.Cost ?? 0;
                
                decimal revenue = 0;
                var moduleTemplates = new List<ModuleRevenueViewModel>();
                
                // Try to find templates for this module
                // matching logic: Module.Name == Helper.GetModuleNameByKey(template.ModuleType)
                if (templatesByModule.TryGetValue(module.Name, out var linkedTemplates))
                {
                    foreach (var t in linkedTemplates)
                    {
                        var guidString = t.TrackingGuid.ToString();
                        var conversionData = conversions.FirstOrDefault(c => c.Source == guidString);

                        if (conversionData != null)
                        {
                            revenue += conversionData.Revenue;
                            moduleTemplates.Add(new ModuleRevenueViewModel
                            {
                                ModuleName = module.Name,
                                TemplateName = t.TemplateName,
                                TotalRevenue = conversionData.Revenue,
                                OrderCount = conversionData.Count
                            });
                        }
                    }
                }
                
                if (cost > 0 || revenue > 0)
                {
                    result.Add(new ModuleFinancialStatsViewModel
                    {
                        ModuleId = module.Id,
                        ModuleName = module.Name,
                        TotalExpenditure = cost,
                        TotalRevenue = revenue,
                        Templates = moduleTemplates.OrderByDescending(t => t.TotalRevenue).ToList()
                    });
                }
            }

            var giftWheelId = await _context.GiftWheels
                .Where(w => w.CompanyId == companyId)
                .Select(w => (int?)w.Id)
                .FirstOrDefaultAsync();

            if (giftWheelId.HasValue)
            {
                var giftWheelRevenue = await _context.GiftWheelSpins
                    .Where(s => s.GiftWheelId == giftWheelId.Value
                                && s.SpinDate >= start && s.SpinDate <= end
                                && s.VoucherUsed && s.OrderAmount.HasValue)
                    .SumAsync(s => s.OrderAmount ?? 0);

                var giftWheelCost = await _context.GiftWheelSpins
                    .Where(s => s.GiftWheelId == giftWheelId.Value
                                && s.SpinDate >= start && s.SpinDate <= end)
                    .SumAsync(s => s.Cost ?? 0);

                if (giftWheelRevenue > 0 || giftWheelCost > 0)
                {
                    result.Add(new ModuleFinancialStatsViewModel
                    {
                        ModuleId = 0,
                        ModuleName = "Hediye Çarkı",
                        TotalRevenue = giftWheelRevenue,
                        TotalExpenditure = giftWheelCost,
                        Templates = new List<ModuleRevenueViewModel>()
                    });
                }
            }

            return result.OrderByDescending(r => r.TotalRevenue)
                         .ThenByDescending(r => r.TotalExpenditure)
                         .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating module revenues for company {CompanyId}", companyId);
            return new List<ModuleFinancialStatsViewModel>();
        }
    }

    /// <summary>
    /// UsageType değerini kanal grubu anahtarına çevirir (SMS, E-posta, WhatsApp vb. tek grupta birleşir).
    /// </summary>
    private static string ChannelGroupKey(string? usageType)
    {
        if (string.IsNullOrWhiteSpace(usageType)) return "diger";
        var n = usageType.Trim();
        if (n.Equals("Sms", StringComparison.OrdinalIgnoreCase) || n.Contains("Sms bildirim", StringComparison.OrdinalIgnoreCase)) return "sms";
        if (n.Equals("Email", StringComparison.OrdinalIgnoreCase) || n.Contains("Email gönderim", StringComparison.OrdinalIgnoreCase)) return "email";
        if (n.Contains("Whatsapp", StringComparison.OrdinalIgnoreCase) && (n.Contains("notification", StringComparison.OrdinalIgnoreCase) || n.Contains("bildirim", StringComparison.OrdinalIgnoreCase))) return "whatsapp_bildirim";
        if (n.Equals("Whatsapp", StringComparison.OrdinalIgnoreCase) || n.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase)) return "whatsapp_bildirim";
        if (n.Equals("Bulk_message", StringComparison.OrdinalIgnoreCase) || n.Contains("Bulk", StringComparison.OrdinalIgnoreCase)) return "toplu_mesaj";
        if (n.Contains("Yorum", StringComparison.OrdinalIgnoreCase) && (n.Contains("taşıma", StringComparison.OrdinalIgnoreCase) || n.Contains("tasima", StringComparison.OrdinalIgnoreCase))) return "yorum_tasima";
        if (n.Contains("Video", StringComparison.OrdinalIgnoreCase) || n.Equals("Video_upload", StringComparison.OrdinalIgnoreCase)) return "video_yukleme";
        if (n.Contains("Hediye çeki", StringComparison.OrdinalIgnoreCase) || n.Equals("Voucher_creation", StringComparison.OrdinalIgnoreCase) || n.Contains("Voucher", StringComparison.OrdinalIgnoreCase)) return "hediye_ceki";
        return "diger";
    }

    /// <summary>
    /// Kanal grubu için Türkçe görüntü adı döndürür.
    /// </summary>
    private static string ChannelTurkishLabel(string? usageType)
    {
        var key = ChannelGroupKey(usageType);
        return key switch
        {
            "sms" => "SMS",
            "email" => "E-posta",
            "whatsapp_bildirim" => "WhatsApp Bildirimi",
            "toplu_mesaj" => "Toplu Mesaj",
            "yorum_tasima" => "Yorum Taşıma",
            "video_yukleme" => "Video Yükleme",
            "hediye_ceki" => "Hediye Çeki Oluşturma",
            _ => "Diğer"
        };
    }
}
