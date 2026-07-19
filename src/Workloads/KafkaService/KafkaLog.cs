using KafkaService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Internals;
using Maelstrom.Models;
using System.Collections.Concurrent;

namespace KafkaService;

internal class KafkaLog(ILogger<KafkaLog> logger, IMaelstromNode _node) : Workload(_node)
{
    private const int _maxReturnedMessages = 10;
    private const int _maxAttempts = 10;
    private readonly ILogger<KafkaLog> logger = logger;
    private readonly SemaphoreSlim _offsetLock = new(1);

    [MaelstromHandler(Send.SendType)]
    public async Task HandleSend(Message message, CancellationToken cancellationToken)
    {
        var send = message.DeserializeAs<Send>().Body;
        logger.LogInformation("Received send request: {Key} {Message}", send.Key, send.Message);
        var offset = await IncrementOffset(send.Key, cancellationToken);
        await WriteLog(send.Key, offset, send.Message, cancellationToken);
        await node.ReplyAsync(message, new SendOk(offset), cancellationToken);
    }

    [MaelstromHandler(Poll.PollType)]
    public async Task HandlePoll(Message message, CancellationToken cancellationToken)
    {
        var poll = message.DeserializeAs<Poll>().Body;
        logger.LogInformation("Received poll request: {Offsets}", poll.Offsets);
        ConcurrentDictionary<string, List<List<int>>> messages = [];
        await Task.WhenAll(
            poll.Offsets
            .Select(async kv => messages[kv.Key] = await GetLogs(kv.Key, kv.Value, cancellationToken)));
        await node.ReplyAsync(message, new PollOk(messages.ToDictionary()), cancellationToken);
    }

    [MaelstromHandler(CommitOffsets.CommitOffsetsType)]
    public async Task HandleCommitOffsets(Message message, CancellationToken cancellationToken)
    {
        var commitOffsets = message.DeserializeAs<CommitOffsets>().Body;
        logger.LogInformation("Received commit offsets request: {Offsets}", commitOffsets.Offsets);
        await node.ReplyAsync(message, new CommitOffsetsOk(), cancellationToken);
        await Task.WhenAll(
            commitOffsets.Offsets
            .Select(kv => UpdateCommittedOffset(kv.Key, kv.Value, cancellationToken)));
    }

    [MaelstromHandler(ListCommittedOffsets.ListCommittedOffsetsType)]
    public async Task HandleListCommittedOffsets(Message message, CancellationToken cancellationToken)
    {
        var listCommittedOffsets = message.DeserializeAs<ListCommittedOffsets>().Body;
        logger.LogInformation("Received list committed offsets request: {Keys}", listCommittedOffsets.Keys);
        Dictionary<string, int> committedOffsets = (await Task.WhenAll(
            listCommittedOffsets.Keys
                .Select(async k => new KeyValuePair<string, int>(k, await GetCommittedOffset(k, cancellationToken)))))
            .ToDictionary();

        await node.ReplyAsync(message, new ListCommittedOffsetsOk(committedOffsets), cancellationToken);
    }

    private static string GetOffsetKey(string key) => $"offsets/{key}";

    private async Task<int> GetLatestOffset(string key, CancellationToken cancellationToken) => await GetCounter(GetOffsetKey(key), cancellationToken);

    private async Task<int> IncrementOffset(string key, CancellationToken cancellationToken)
    {
        await _offsetLock.WaitAsync(cancellationToken);
        try
        {
            return await IncrementCounter(GetOffsetKey(key), cancellationToken);
        }
        finally
        {
            _offsetLock.Release();
        }
    }

    private static string GetCommittedKey(string key) => $"committed/{key}";

    private async Task<int> GetCommittedOffset(string key, CancellationToken cancellationToken) => await GetCounter(GetCommittedKey(key), cancellationToken);

    private async Task UpdateCommittedOffset(string key, int value, CancellationToken cancellationToken)
    {
        string committedKey = GetCommittedKey(key);
        int attempts = 1;
        while (!cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Get counter {key}, attempt {attempts}", committedKey, attempts);
            var offset = await GetCounter(committedKey, cancellationToken);
            if (value <= offset)
            {
                logger.LogDebug("New offset {value} is not greater than current offset {offset}, skipping", value, offset);
                return;
            }

            try
            {
                await node.LinKvStoreClient.CasAsync(committedKey, offset, value, createIfNotExists: true, cancellationToken: cancellationToken);
            }
            catch (KvStoreCasPreconditionFailed)
            {
                logger.LogWarning("CAS failed, waiting and retrying");
                await Task.Delay(10 + new Random().Next(-2, 2), cancellationToken);
                attempts++;
                continue;
            }

            logger.LogDebug("Increment succeeded, new {key} = {offset}", key, committedKey);
            return;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException("UpdateCommittedOffset failed: task was cancelled");
        }
    }

    private async Task<int> GetCounter(string key, CancellationToken cancellationToken) =>
        await node.LinKvStoreClient.ReadOrDefaultAsync(key, 0, cancellationToken);

    private async Task<int> IncrementCounter(string key, CancellationToken cancellationToken) =>
        await node.LinKvStoreClient.SafeUpdateAsync(key, v => v + 1, 0, maxAttempts: _maxAttempts, cancellationToken: cancellationToken);

    private static string GetLogKey(string key, int offset) => $"logs/{key}/{offset}";

    private async Task WriteLog(string key, int offset, int message, CancellationToken cancellationToken)
    {
        logger.LogDebug("Writing log: {Key} {Offset} {Message}", key, offset, message);
        await node.SeqKvStoreClient.WriteAsync(GetLogKey(key, offset), message, cancellationToken: cancellationToken);
    }

    private async Task<List<List<int>>> GetLogs(string key, int offset, CancellationToken cancellationToken)
    {
        var maxOffset = await GetLatestOffset(key, cancellationToken);
        var logs = new List<List<int>>();
        while (logs.Count < _maxReturnedMessages && offset <= maxOffset)
        {
            try
            {
                var log = await node.SeqKvStoreClient.ReadAsync<string, int>(GetLogKey(key, offset), cancellationToken: cancellationToken);
                logs.Add([offset, log]);
                offset++;
            }
            catch (KvStoreKeyNotFoundException)
            {
                logger.LogWarning("Log at offset {offset} not found, skipping", offset);
                offset++;
                continue;
            }
        }

        return logs;
    }
}