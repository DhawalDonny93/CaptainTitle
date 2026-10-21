using AppBackendBasic.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppBackendBasic.Services.BackgroundJobs;

/// <summary>
/// Background service that processes scheduled notifications every minute
/// </summary>
public class NotificationProcessorJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationProcessorJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public NotificationProcessorJob(
        IServiceProvider serviceProvider,
        ILogger<NotificationProcessorJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Processor Job started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Notification Processor Job");
            }

            // Wait for next interval
            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Notification Processor Job stopped");
    }

    private async Task ProcessNotificationsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var schedulerService = scope.ServiceProvider
                .GetRequiredService<INotificationSchedulerService>();

            await schedulerService.ProcessScheduledNotificationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled notifications");
        }
    }
}
