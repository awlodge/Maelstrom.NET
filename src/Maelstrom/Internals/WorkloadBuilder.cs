namespace Maelstrom.Internals;

internal class WorkloadBuilder(IMaelstromNode node, IKvStoreClientFactory kvStoreClientFactory) : IWorkloadBuilder
{
    IMaelstromNode IWorkloadBuilder.Node => node;
    IKvStoreClientFactory IWorkloadBuilder.KvStoreClientFactory => kvStoreClientFactory;
}
