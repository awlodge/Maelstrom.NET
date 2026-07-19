using CounterService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Models;

namespace CounterService;

internal class Counter(ILogger<Counter> logger, IMaelstromNode _node) : Workload(_node)
{
    private const string _counterKey = "counter";
    private const int _maxAttempts = 10;
    private readonly ILogger<Counter> logger = logger;

    [MaelstromHandler(Read.ReadType)]
    public async Task HandleRead(Message message, CancellationToken cancellationToken)
    {
        logger.LogDebug("Received counter read");

        // Increment by 0 to force read of latest value from store.
        var latestValue = await IncrementValue(0, cancellationToken);
        logger.LogInformation("Counter read OK, value {value}", latestValue);
        await node.ReplyAsync(message, new ReadOk<int>(latestValue), cancellationToken);
    }

    [MaelstromHandler(Add.AddType)]
    public async Task HandleAdd(Message message, CancellationToken cancellationToken)
    {
        var add = message.DeserializeAs<Add>().Body;
        logger.LogDebug("Received counter add {delta}", add.Delta);
        await node.ReplyAsync(message, new AddOk(), cancellationToken);
        var latestValue = await IncrementValue(add.Delta, cancellationToken);
        logger.LogInformation("Counter incremented by {delta} to {val}", add.Delta, latestValue);
    }

    private async Task<int> IncrementValue(int delta, CancellationToken cancellationToken) =>
        await node.SeqKvStoreClient.SafeUpdateAsync(_counterKey,
            v => v + delta,
            0,
            maxAttempts: _maxAttempts,
            cancellationToken: cancellationToken);
}
