namespace Maelstrom;

public class Workload
{
    protected readonly IMaelstromNode node;
    protected readonly IKvStoreClient seqKvStoreClient;
    protected readonly IKvStoreClient linKvStoreClient;

    public Workload(IWorkloadBuilder builder)
    {
        node = builder.Node;
        seqKvStoreClient = builder.KvStoreClientFactory.Create("seq-kv", node);
        linKvStoreClient = builder.KvStoreClientFactory.Create("lin-kv", node);
    }
}
