using EchoService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Models;

namespace EchoService;

internal class EchoServer(ILogger<EchoServer> logger, IWorkloadBuilder builder) : Workload(builder)
{
    private readonly ILogger<EchoServer> logger = logger;

    [MaelstromHandler<Echo>]
    public async Task HandleEcho(Message<Echo> message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Echoing message: {EchoMessage}", message.Body.EchoMessage);
        await node.ReplyAsync(message, new EchoOk(message.Body.EchoMessage), cancellationToken);
    }
}
