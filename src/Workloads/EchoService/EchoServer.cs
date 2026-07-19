using EchoService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Models;

namespace EchoService;

internal class EchoServer(ILogger<EchoServer> logger, IMaelstromNode _node) : Workload(_node)
{
    private readonly ILogger<EchoServer> logger = logger;

    [MaelstromHandler(Echo.EchoType)]
    public async Task HandleEcho(Message message, CancellationToken cancellationToken = default)
    {
        var echo = message.DeserializeAs<Echo>().Body;
        logger.LogInformation("Echoing message: {EchoMessage}", echo.EchoMessage);
        await node.ReplyAsync(message, new EchoOk(echo.EchoMessage), cancellationToken);
    }
}
