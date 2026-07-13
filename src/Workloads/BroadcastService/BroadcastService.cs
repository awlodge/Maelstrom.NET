using BroadcastService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Models;

namespace BroadcastService;

internal class BroadcastService(ILogger<BroadcastService> logger, IMaelstromNode node) : Workload(node)
{
    private readonly ILogger<BroadcastService> logger = logger;
    private readonly HashSet<int> _broadcastMessages = [];
    private Dictionary<string, string[]> _topology = [];

    [MaelstromHandler(Broadcast.BroadcastType)]
    public async Task HandleBroadcast(Message message, CancellationToken cancellationToken)
    {
        var broadcastMessage = message.DeserializeAs<Broadcast>().Body.BroadcastMessage;
        logger.LogInformation("Received broadcast message: {BroadcastMessage}", broadcastMessage);
        await Node.ReplyAsync(message, new BroadcastOk());
        if (_broadcastMessages.Contains(broadcastMessage))
        {
            logger.LogInformation("Message already seen, ignoring broadcast");
        }
        else
        {
            var nextHops = GetNextHops(message);
            if (nextHops.Count > 0)
            {
                logger.LogInformation("Broadcasting message to next hops {nextHops}", nextHops);
                await Task.WhenAll(nextHops.Select(n => node.RpcAsync(n, new Broadcast(broadcastMessage))));
            }
            logger.LogInformation("Message broadcast successfully");
            _broadcastMessages.Add(broadcastMessage);
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

    private List<string> GetNextHops(Message message)
    {
        // Return neighbors excluding message source to avoid reflection.
        return _topology.TryGetValue(Node.NodeId, out var neighbors)
            ? neighbors.Where(n => n != message.Src).ToList()
            : [];
    }
}
