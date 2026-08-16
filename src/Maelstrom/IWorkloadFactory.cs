using Maelstrom.Internals;

namespace Maelstrom;

public interface IWorkloadFactory
{
    internal IMaelstromNode Node { get; }
    internal IKvStoreClientFactory KvStoreClientFactory { get; }
}
