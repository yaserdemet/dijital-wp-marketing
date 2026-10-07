using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.Modules.Birthday;
using System.Text.Json;

namespace PushDashboard.Services;

public class BirthdayBackgroundService : BackgroundService
{
    private readonly ILogger<BirthdayBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(3);
    private readonly int[] _activeSlots = new[] {0, 3, 6, 9, 12, 15, 18, 21 };

    public BirthdayBackgroundService(
        ILogger<BirthdayBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private DateTime GetTurkeyTime()
    {
        return DateTime.Now;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Birthday Background Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = GetTurkeyTime();
                var currentHour = now.Hour;

                if (IsActiveSlot(currentHour))
                {
                    await ProcessSlotAsync(currentHour, stoppingToken);
                }
                else
                {
                    _logger.LogDebug("Current hour {Hour} is not an active slot, skipping", currentHour);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in birthday background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private bool IsActiveSlot(int hour)
    {
        return _activeSlots.Contains(hour);
    }

    private async Task ProcessSlotAsync(int slotHour, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var birthdayService = scope.ServiceProvider.GetRequiredService<IBirthdayModuleService>();
        var activityLogService = scope.ServiceProvider.GetRequiredService<IActivityLogService>();

        try
        {
            var now = GetTurkeyTime();
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);

            var birthdayModule = await context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Doğum Günü Hatırlatma" && m.IsActive, cancellationToken);

            if (birthdayModule == null)
            {
                return;
            }

            var windowStartHour = slotHour;
            var windowEndHour = slotHour + 3;
            if (windowEndHour > 24) windowEndHour = 24;

            _logger.LogInformation("Processing slot {SlotHour}:00 - {WindowEndHour}:00 (Turkey time: {Now})",
                slotHour, windowEndHour, now);

            var companiesWithModule = await context.CompanyModules
                .Include(cm => cm.Settings)
                .Where(cm => cm.ModuleId == birthdayModule.Id &&
                           cm.IsOwned &&
                           cm.IsActive &&
                           !cm.IsPassive)
                .ToListAsync(cancellationToken);

            var companiesToProcess = new List<(CompanyModule CompanyModule, TimeSpan SendTime, string SendTimeStr)>();

            foreach (var companyModule in companiesWithModule)
            {
                try
                {
                    if (companyModule.Settings == null)
                        continue;

                    var settings = companyModule.Settings.Settings;
                    if (settings == null)
                        continue;

                    var enabled = GetBoolSetting(settings, "enabled", true);
                    if (!enabled)
                        continue;

                    var autoSend = GetBoolSetting(settings, "autoSend", false);
                    if (!autoSend)
                        continue;

                    var sendTimeStr = GetStringSetting(settings, "sendTime", "09:00");
                    if (!TimeSpan.TryParse(sendTimeStr, out var sendTime))
                    {
                        _logger.LogWarning("Invalid sendTime format '{SendTime}' for company {CompanyId}, using default 09:00",
                            sendTimeStr, companyModule.CompanyId);
                        sendTime = new TimeSpan(9, 0, 0);
                    }

                    var alreadySentToday = await context.BirthdayNotifications
                        .AnyAsync(bn => bn.CompanyId == companyModule.CompanyId &&
                                       bn.IsAutomatic &&
                                       bn.SentAt >= todayStart.ToUniversalTime() &&
                                       bn.SentAt < todayEnd.ToUniversalTime(),
                                       cancellationToken);

                    if (alreadySentToday)
                    {
                        continue;
                    }

                    var sendHour = sendTime.Hours;
                    var isInWindow = false;

                    if (windowEndHour > windowStartHour)
                    {
                        isInWindow = sendHour >= windowStartHour && sendHour < windowEndHour;
                    }
                    else
                    {
                        isInWindow = sendHour >= windowStartHour || sendHour < windowEndHour;
                    }

                    if (isInWindow)
                    {
                        var currentTime = now.TimeOfDay;
                        var timeDifference = Math.Abs((currentTime - sendTime).TotalMinutes);

                        if (timeDifference <= 5)
                        {
                            companiesToProcess.Add((companyModule, sendTime, sendTimeStr));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing company {CompanyId} for slot {SlotHour}",
                        companyModule.CompanyId, slotHour);
                }
            }

            _logger.LogInformation("Found {Count} companies to process in slot {SlotHour}:00",
                companiesToProcess.Count, slotHour);

            var tasks = companiesToProcess.Select(item => ProcessCompanyBirthdayNotificationAsync(
                item.CompanyModule.CompanyId,
                item.SendTime,
                item.SendTimeStr,
                todayStart,
                todayEnd,
                cancellationToken));

            await Task.WhenAll(tasks);

            _logger.LogInformation("Completed processing slot {SlotHour}:00, processed {Count} companies",
                slotHour, companiesToProcess.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessSlotAsync for slot {SlotHour}", slotHour);
        }
    }

    private async Task ProcessCompanyBirthdayNotificationAsync(
        Guid companyId,
        TimeSpan sendTime,
        string sendTimeStr,
        DateTime todayStart,
        DateTime todayEnd,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var birthdayService = scope.ServiceProvider.GetRequiredService<IBirthdayModuleService>();
        var activityLogService = scope.ServiceProvider.GetRequiredService<IActivityLogService>();

        try
        {
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;
            var timeDifference = Math.Abs((currentTime - sendTime).TotalMinutes);

            if (timeDifference > 5)
            {
                _logger.LogDebug("Company {CompanyId} send time ({SendTime}) does not match current time ({CurrentTime}), difference: {Difference} minutes, skipping",
                    companyId, sendTime, currentTime, timeDifference);
                return;
            }

            var alreadySentToday = await context.BirthdayNotifications
                .AnyAsync(bn => bn.CompanyId == companyId &&
                               bn.IsAutomatic &&
                               bn.SentAt >= todayStart.ToUniversalTime() &&
                               bn.SentAt < todayEnd.ToUniversalTime(),
                               cancellationToken);

            if (alreadySentToday)
            {
                _logger.LogDebug("Company {CompanyId} already processed today, skipping", companyId);
                return;
            }

            _logger.LogInformation("Processing birthday notifications for company {CompanyId} at {Time} (send time: {SendTime})",
                companyId, currentTime, sendTime);

            var result = await birthdayService.SendBirthdayNotificationsAsync(companyId, "30eb145f-1cbf-4250-8efa-829f617fac72");

            if (result.Success)
            {
                _logger.LogInformation("Birthday notifications sent for company {CompanyId}. Sent: {Count}, Cost: {Cost}",
                    companyId, result.NotificationsSent, result.TotalCost);

                if (result.NotificationsSent > 0)
                {
                    await activityLogService.LogActivityAsync(
                        "system",
                        companyId,
                        ActivityActionTypes.Send,
                        ActivityModules.Notification,
                        $"Otomatik doğum günü bildirimleri gönderildi ({result.NotificationsSent} adet)",
                        entityType: "BirthdayNotification",
                        details: new {
                            NotificationsSent = result.NotificationsSent,
                            TotalCost = result.TotalCost,
                            IsAutomatic = true,
                            SendTime = sendTimeStr,
                            SlotHour = now.Hour
                        });
                }
            }
            else
            {
                _logger.LogWarning("Failed to send birthday notifications for company {CompanyId}: {Message}",
                    companyId, result.Message);

                await activityLogService.LogActivityAsync(
                    "system",
                    companyId,
                    ActivityActionTypes.Send,
                    ActivityModules.Notification,
                    $"Otomatik doğum günü bildirimleri gönderilemedi: {result.Message}",
                    entityType: "BirthdayNotification",
                    details: new {
                        Error = result.Message,
                        IsAutomatic = true,
                        SendTime = sendTimeStr,
                        SlotHour = now.Hour
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing birthday notification for company {CompanyId}",
                companyId);
        }
    }

    private bool GetBoolSetting(Dictionary<string, object> settings, string key, bool defaultValue)
    {
        if (settings.TryGetValue(key, out var value))
        {
            if (value is bool boolValue)
                return boolValue;
            if (value is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.True)
                return true;
            if (value is System.Text.Json.JsonElement jsonElementFalse && jsonElementFalse.ValueKind == System.Text.Json.JsonValueKind.False)
                return false;
            if (bool.TryParse(value?.ToString(), out var parsed))
                return parsed;
        }
        return defaultValue;
    }

    private string GetStringSetting(Dictionary<string, object> settings, string key, string defaultValue)
    {
        if (settings.TryGetValue(key, out var value))
        {
            return value?.ToString() ?? defaultValue;
        }
        return defaultValue;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Birthday Background Service is stopping");
        await base.StopAsync(stoppingToken);
    }
}
