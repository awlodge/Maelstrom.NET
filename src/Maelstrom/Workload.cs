namespace Maelstrom;

public class Workload
{
    protected readonly IMaelstromNode node;
    protected readonly IKvStoreClient seqKvStoreClient;
    protected readonly IKvStoreClient linKvStoreClient;

    public Workload(IWorkloadFactory workloadFactory)
    {
        node = workloadFactory.Node;
        seqKvStoreClient = workloadFactory.KvStoreClientFactory.Create("seq-kv", node);
        linKvStoreClient = workloadFactory.KvStoreClientFactory.Create("lin-kv", node);
    }
}
