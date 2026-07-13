
namespace BroadcastService;

internal class BroadcastServicePoller(BroadcastService broadcastService) : BackgroundService
{
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_pollInterval, stoppingToken);
            if (!stoppingToken.IsCancellationRequested)
            {
                await broadcastService.PollNeighborsAsync(stoppingToken);
            }
        }
    }
}
