using Maelstrom.Internals;

namespace Maelstrom;

internal interface IMaelstromClientFactory
{
    MaelstromClient CreateMaelstromClient(string nodeId);
}
