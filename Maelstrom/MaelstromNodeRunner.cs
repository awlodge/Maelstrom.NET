using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maelstrom;

internal class MaelstromNodeRunner(ILogger<MaelstromNodeRunner> logger, MaelstromNode node) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting...");
        await node.RunAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping...");
        node.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
