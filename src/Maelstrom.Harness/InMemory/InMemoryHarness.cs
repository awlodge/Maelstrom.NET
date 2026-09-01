
using Microsoft.Extensions.Logging;

namespace Maelstrom.Harness.InMemory;

public class InMemoryHarness(MaelstromBus maelstromBus, ILogger<InMemoryHarness> logger) : MaelstromHarness(maelstromBus, logger)
{
    private readonly ILogger<InMemoryHarness> _logger = logger;
    private InMemoryKvStore? _linKvStore;
    private InMemoryKvStore? _seqKvStore;

    protected override (IMaelstromClient, INode) CreateMaelstromClient(string nodeId)
    {
        var node = new InMemoryClientRunner(nodeId);
        node.Start();
        return (node.Client, node);
    }

    protected override INode CreateLinKvStore()
    {
        var node = new InMemoryClientRunner(LinKvStoreId);
        node.Start();
        _linKvStore = new InMemoryKvStore(node.Client);
        node.AddMessageHandlers(MaelstromHandlerAttribute.GetHandlers(_linKvStore));
        return node;
    }

    protected override INode CreateSeqKvStore()
    {
        var node = new InMemoryClientRunner(SeqKvStoreId);
        node.Start();
        _seqKvStore = new InMemoryKvStore(node.Client);
        node.AddMessageHandlers(MaelstromHandlerAttribute.GetHandlers(_seqKvStore));
        return node;
    }

    public InMemoryHarness AddWorkload<TWorkload>() where TWorkload : Workload
    {
        _logger.LogInformation("Starting workload {Workload}", typeof(TWorkload));
        var runner = new InMemoryWorkloadRunner<TWorkload>();
        runner.Start();
        AddWorkloadNode(runner);
        return this;
    }
}
