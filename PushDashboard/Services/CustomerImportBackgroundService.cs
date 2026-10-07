using Microsoft.Extensions.DependencyInjection;

namespace PushDashboard.Services;

public class CustomerImportBackgroundService : BackgroundService
{
    private readonly ILogger<CustomerImportBackgroundService> _logger;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;

    public CustomerImportBackgroundService(
        ILogger<CustomerImportBackgroundService> logger,
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Customer Import Background Service is starting");

        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    await workItem(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing work item");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation token is triggered
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in background processing");
                
                // Wait a bit before retrying to avoid tight loop on persistent errors
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Customer Import Background Service is stopping");
        await base.StopAsync(stoppingToken);
    }
}
