using BroadcastService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Internals;
using Maelstrom.Models;
using Microsoft.Extensions.Options;

namespace BroadcastService;

internal class BroadcastService(ILogger<BroadcastService> logger, IMaelstromNode node, IOptions<BroadcastServiceOptions> options) : Workload(node)
{
    private readonly ILogger<BroadcastService> logger = logger;
    private readonly HashSet<int> _broadcastMessages = [];
    private Dictionary<string, string[]> _topology = [];
    private readonly TimeSpan _rpcTimeout = options.Value.RpcTimeout;
    private readonly TimeSpan _rpcRetryDelay = options.Value.RpcRetryDelay;

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

    private async Task BroadcastAsync(string neighbor, int broadcastMessage, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await node.RpcAsync(neighbor, new Broadcast(broadcastMessage), timeout: _rpcTimeout, cancellationToken: cancellationToken);
                return;
            }
            catch (RpcFailedException ex)
            {
                logger.LogError(ex, "Broadcast to node {Neighbor} failed", neighbor);
            }
            await Task.Delay(_rpcRetryDelay, cancellationToken);
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
