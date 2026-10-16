using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AcxiomCRM.Services;

namespace AcxiomCRM.Services;

public class FollowUpBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FollowUpBackgroundService> _logger;

    public FollowUpBackgroundService(IServiceProvider serviceProvider, ILogger<FollowUpBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FollowUpBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var followUpService = scope.ServiceProvider.GetRequiredService<IFollowUpService>();
                await followUpService.MarkOverdueFollowUpsAsMissedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing FollowUpBackgroundService.");
            }

            // Run check every 30 minutes
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }

        _logger.LogInformation("FollowUpBackgroundService is stopping.");
    }
}
