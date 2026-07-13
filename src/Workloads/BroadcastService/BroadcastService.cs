using BroadcastService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Internals;
using Maelstrom.Models;

namespace BroadcastService;

internal class BroadcastService(ILogger<BroadcastService> logger, IMaelstromNode node) : Workload(node)
{
    private readonly ILogger<BroadcastService> logger = logger;
    private readonly HashSet<int> _broadcastMessages = [];
    private Dictionary<string, string[]> _topology = [];
    private readonly TimeSpan _rpcTimeout = TimeSpan.FromSeconds(1);

    [MaelstromHandler(Broadcast.BroadcastType)]
    public async Task HandleBroadcast(Message message, CancellationToken cancellationToken)
    {
        var broadcastMessage = message.DeserializeAs<Broadcast>().Body.BroadcastMessage;
        logger.LogInformation("Received broadcast message: {BroadcastMessage}", broadcastMessage);
        await Node.ReplyAsync(message, new BroadcastOk(), cancellationToken);
        if (_broadcastMessages.Contains(broadcastMessage))
        {
            logger.LogInformation("Message already seen, ignoring broadcast");
        }
        else
        {
            _broadcastMessages.Add(broadcastMessage);
            var nextHops = GetNextHops(message);
            if (nextHops.Count > 0)
            {
                logger.LogInformation("Broadcasting message to next hops {nextHops}", nextHops);
                await Task.WhenAll(nextHops.Select(n => BroadcastAsync(n, broadcastMessage, cancellationToken)));
            }
            logger.LogInformation("Message broadcast successfully");
        }
    }

    [MaelstromHandler(Read.ReadType)]
    public async Task HandleRead(Message message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received read request");
        await Node.ReplyAsync(message, new ReadOk([.. _broadcastMessages]));
    }

    [MaelstromHandler(Topology.TopologyType)]
    public async Task HandleTopology(Message message, CancellationToken cancellationToken)
    {
        var topologyMessage = message.DeserializeAs<Topology>().Body;
        logger.LogInformation("Received topology: {topology}", topologyMessage.TopologyData);
        var topology = topologyMessage.TopologyData;
        if (topology == null)
        {
            await Node.ErrorAsync(message, ErrorCodes.MalformedRequest, "Malformed topology data");
            throw new Exception($"Malformed topology data: {topologyMessage.TopologyData}");
        }
        _topology = topology;
        await Node.ReplyAsync(message, new TopologyOk());
    }

    public async Task PollNeighborsAsync(CancellationToken cancellationToken)
    {
        if (Neighbors.Count > 0)
        {
            logger.LogInformation("Poll neighbors: {Neighbors}", Neighbors);
            await Task.WhenAll(Neighbors.Select(n => PollNeighborAsync(n, cancellationToken)));
        }
    }

    private async Task PollNeighborAsync(string neighbor, CancellationToken cancellationToken)
    {
        Message? resp = null;
        try
        {
            resp = await node.RpcAsync(neighbor, new Read(), timeout: _rpcTimeout, cancellationToken: cancellationToken);
        }
        catch (RpcFailedException ex)
        {
            logger.LogError(ex, "Read to node {Neighbor} failed", neighbor);
            return;
        }
        var readOk = resp?.DeserializeAs<ReadOk>()?.Body;
        if (readOk is not null)
        {
            var readMessages = readOk.ReadMessages;
            logger.LogInformation("Received update from neighbor {Neighbor}: {Update}", neighbor, readMessages);
            foreach (var m in readMessages)
            {
                _broadcastMessages.Add(m);
            }
        }
    }

    private async Task BroadcastAsync(string neighbor, int broadcastMessage, CancellationToken cancellationToken)
    {
        try
        {
            await node.RpcAsync(neighbor, new Broadcast(broadcastMessage), timeout: _rpcTimeout, cancellationToken: cancellationToken);
        }
        catch (RpcFailedException ex)
        {
            logger.LogError(ex, "Broadcast to node {Neighbor} failed", neighbor);
        }
    }

    private List<string> Neighbors => _topology.TryGetValue(Node.NodeId, out var neighbors)
        ? [.. neighbors]
        : [];

    private List<string> GetNextHops(Message message)
    {
        // Return neighbors excluding message source to avoid reflection.
        return Neighbors.Where(n => n != message.Src).ToList();
    }
}
