using Maelstrom.Models.MessageBodies;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Maelstrom.Harness;

public abstract class MaelstromHarness : IAsyncDisposable
{
    protected const string SeqKvStoreId = "seq-kv";
    protected const string LinKvStoreId = "lin-kv";

    private readonly ILogger _logger;
    private readonly MaelstromBus _maelstromBus;
    private readonly ConcurrentDictionary<string, INode> _workloadNodes = [];
    private readonly ConcurrentBag<INode> _clientNodes = [];
    private IMaelstromClient? _client;

    public MaelstromHarness(MaelstromBus maelstromBus, ILogger logger)
    {
        _logger = logger;
        _maelstromBus = maelstromBus;
    }

    public IMaelstromClient Client => _client ?? throw new Exception("Harness not yet started");
    public IEnumerable<string> WorkloadNodeIds => _workloadNodes.Keys;

    public async Task<MaelstromHarness> StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Harness...");
        AddClient();
        AddSeqKvStore();
        AddLinKvStore();
        _maelstromBus.Start();

        await Task.WhenAll(_workloadNodes.Keys.Select(async n => await InitAsync(n, cancellationToken)));
        _logger.LogInformation("Harness ready");
        return this;
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping Harness...");
        await _maelstromBus.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await StopAsync();
        await Task.WhenAll(_workloadNodes.Values.Select(async n => await n.DisposeAsync()));
        _workloadNodes.Clear();
        await Task.WhenAll(_clientNodes.Select(async n => await n.DisposeAsync()));
        _clientNodes.Clear();

        _client = null;
    }

    public MaelstromHarness AddWorkloadNode(INode node)
    {
        var nodeCount = _workloadNodes.Count + 1;
        var nodeId = $"n{nodeCount}";
        _logger.LogInformation("Adding node {NodeId}", nodeId);
        _maelstromBus.AddNode(nodeId, node.Output, node.Input);
        _workloadNodes[nodeId] = node;
        return this;
    }

    protected abstract (IMaelstromClient, INode) CreateMaelstromClient(string nodeId);
    protected abstract INode CreateSeqKvStore();
    protected abstract INode CreateLinKvStore();

    private MaelstromHarness AddClient()
    {
        var (client, node) = CreateMaelstromClient("c1");
        _client = client;
        _clientNodes.Add(node);
        _maelstromBus.AddNode("c1", node.Output, node.Input);
        return this;
    }

    private void AddSeqKvStore()
    {
        var seqKvStore = CreateSeqKvStore();
        _clientNodes.Add(seqKvStore);
        _maelstromBus.AddNode(SeqKvStoreId, seqKvStore.Output, seqKvStore.Input);
    }

    private void AddLinKvStore()
    {
        var linKvStore = CreateLinKvStore();
        _clientNodes.Add(linKvStore);
        _maelstromBus.AddNode(LinKvStoreId, linKvStore.Output, linKvStore.Input);
    }

    private async Task InitAsync(string nodeId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending Init from {SrcNodeId} to {DstNodeId}", Client.NodeId, nodeId);
        await Client.RpcAsync<Init, InitOk>(
                nodeId,
                new Init(nodeId, [.. _workloadNodes.Keys]),
                cancellationToken: cancellationToken);
        _logger.LogInformation("Received InitOk from {DstNodeId}", nodeId);
    }
}

public interface INode : IAsyncDisposable
{
    ISender Input { get; }
    IReceiver Output { get; }
}
