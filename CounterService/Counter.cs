using CounterService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Interfaces;
using Maelstrom.Models;

namespace CounterService;

internal class Counter(ILogger<Counter> logger, IMaelstromNode node) : Workload(node)
{
    private const string _counterKey = "counter";
    private const int _maxAttempts = 10;
    private readonly ILogger<Counter> logger = logger;

    [MaelstromHandler(Read.ReadType)]
    public async Task HandleRead(Message message)
    {
        logger.LogDebug("Received counter read");

        // Increment by 0 to force read of latest value from store.
        var latestValue = await IncrementValue(0);
        logger.LogInformation("Counter read OK, value {value}", latestValue);
        await node.ReplyAsync(message, new ReadOk<int>(latestValue));
    }

    [MaelstromHandler(Add.AddType)]
    public async Task HandleAdd(Message message)
    {
        var add = message.DeserializeAs<Add>().Body;
        logger.LogDebug("Received counter add {delta}", add.Delta);
        await node.ReplyAsync(message, new AddOk());
        var latestValue = await IncrementValue(add.Delta);
        logger.LogInformation("Counter incremented by {delta} to {val}", add.Delta, latestValue);
    }

    private async Task<int> IncrementValue(int delta) => await node.SeqKvStoreClient.SafeUpdateAsync(_counterKey, v => v + delta, 0, maxAttempts: _maxAttempts);
}
