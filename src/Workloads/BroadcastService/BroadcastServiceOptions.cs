namespace BroadcastService;

internal record BroadcastServiceOptions
{
    public TimeSpan RpcTimeout { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan RpcRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}
