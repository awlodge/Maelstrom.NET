using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;
using Microsoft.Extensions.Logging;

namespace Maelstrom.Internals;

internal class MaelstromNode(ILogger<MaelstromNode> logger, IReceiver receiver, ISender sender) : MaelstromClientBase(logger, receiver, sender), IMaelstromNode
{
    private readonly ILogger<MaelstromNode> logger = logger;
    private string _nodeId = "";
    private string[] _nodeIds = [];

    public override string NodeId => _nodeId;
    public string[] NodeIds => _nodeIds;

    protected override async Task InitAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Awaiting init message");
        var message = await RecvAsync(cancellationToken);
        if (message == null || message.Body == null)
        {
            throw new Exception("Failed to receive init message");
        }
        if (!message.TryDeserializeAs<Init>(out var initMessage))
        {
            await this.ErrorAsync(message, ErrorCodes.MalformedRequest, "First message must be an init message", cancellationToken);
            throw new Exception("First message must be an init message");
        }
        var init = initMessage.Body;
        _nodeId = init.NodeId;
        _nodeIds = init.NodeIds;
        logger.LogInformation("Node initialized. Node ID: {NodeId}", NodeId);
        await this.ReplyAsync(message, new InitOk(), cancellationToken);
    }
}
