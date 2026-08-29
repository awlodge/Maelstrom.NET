using Microsoft.Extensions.Logging;

namespace Maelstrom.Internals;

internal class MaelstromClient(string nodeId, ILogger<MaelstromClient> logger, IReceiver receiver, ISender sender) : MaelstromClientBase(logger, receiver, sender)
{
    public override string NodeId => nodeId;
}