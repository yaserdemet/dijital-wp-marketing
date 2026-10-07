using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Services.Modules.StockReminder;

namespace PushDashboard.Services;

public class StockReminderBackgroundService : BackgroundService
{
    private readonly ILogger<StockReminderBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Webhook handles real-time; this is a fallback safety net

    public StockReminderBackgroundService(
        ILogger<StockReminderBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock Reminder Background Service is starting");

        // İlk çalışmada 30 saniye bekle (uygulama başlangıcında yüklenme tamamlansın)
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Stock Reminder Background Service - starting cycle at {Time}", DateTime.UtcNow);
                await ProcessAllCompaniesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in stock reminder background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessAllCompaniesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stockReminderService = scope.ServiceProvider.GetRequiredService<IStockReminderModuleService>();

        try
        {
            // Stok Hatırlatma modülünü bul
            var stockReminderModule = await context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Stok Hatırlatma" && m.IsActive, cancellationToken);

            if (stockReminderModule == null)
            {
                _logger.LogDebug("Stock Reminder module not found or not active, skipping");
                return;
            }

            // Bu modüle sahip tüm aktif şirketleri getir
            var companiesWithModule = await context.CompanyModules
                .Where(cm => cm.ModuleId == stockReminderModule.Id &&
                           cm.IsOwned &&
                           cm.IsActive &&
                           !cm.IsPassive)
                .Select(cm => cm.CompanyId)
                .Distinct()
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} companies with active stock reminder module", companiesWithModule.Count);

            foreach (var companyId in companiesWithModule)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    _logger.LogInformation("Processing stock reminders for company {CompanyId}", companyId);

                    // Stok alarm ürünlerini çek ve stoğu gelen ürünlerin abonelerine mesaj gönder
                    var result = await stockReminderService.SendStockReminderMessagesAsync(companyId);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Stock reminder processing completed for company {CompanyId}: {Products} products, {Notifications} notifications sent, {Cost} cost",
                            companyId, result.TotalProducts, result.TotalNotificationsSent, result.TotalCost);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Stock reminder processing failed for company {CompanyId}: {Message}",
                            companyId, result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing stock reminders for company {CompanyId}", companyId);
                }

                // Şirketler arası kısa bekleme (API rate limit koruması)
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessAllCompaniesAsync");
        }
    }
}
