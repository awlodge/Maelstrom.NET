namespace Maelstrom.Internals;

internal class WorkloadFactory(IMaelstromNode node, IKvStoreClientFactory kvStoreClientFactory) : IWorkloadFactory
{
    IMaelstromNode IWorkloadFactory.Node => node;
    IKvStoreClientFactory IWorkloadFactory.KvStoreClientFactory => kvStoreClientFactory;
}
