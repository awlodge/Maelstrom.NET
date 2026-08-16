using Maelstrom.Internals;

namespace Maelstrom;

public interface IWorkloadBuilder
{
    internal IMaelstromNode Node { get; }
    internal IKvStoreClientFactory KvStoreClientFactory { get; }
}
