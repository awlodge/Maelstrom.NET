namespace BroadcastService;

internal record BroadcastServiceOptions
{
    public TopologyStrategy BroadcastTopologyStrategy { get; set; } = TopologyStrategy.UseProvidedTopology;

    public TimeSpan RpcTimeout { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan RpcRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}

internal enum TopologyStrategy
{
    AllNodes,
    UseProvidedTopology,
}