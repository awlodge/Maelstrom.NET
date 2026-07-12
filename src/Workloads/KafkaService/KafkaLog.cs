using KafkaService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Interfaces;
using Maelstrom.Models;

namespace KafkaService;

internal class KafkaLog(ILogger<KafkaLog> logger, IMaelstromNode node) : Workload(node)
{
    private const int _maxReturnedMessages = 10;
    private const int _maxAttempts = 10;
    private readonly ILogger<KafkaLog> logger = logger;
    private readonly SemaphoreSlim _offsetLock = new(1);

    [MaelstromHandler(Send.SendType)]
    public async Task HandleSend(Message message)
    {
        var send = message.DeserializeAs<Send>().Body;
        logger.LogInformation("Received send request: {Key} {Message}", send.Key, send.Message);
        var offset = await IncrementOffset(send.Key);
        await WriteLog(send.Key, offset, send.Message);
        await node.ReplyAsync(message, new SendOk(offset));
    }

    [MaelstromHandler(Poll.PollType)]
    public async Task HandlePoll(Message message)
    {
        var poll = message.DeserializeAs<Poll>().Body;
        logger.LogInformation("Received poll request: {Offsets}", poll.Offsets);
        Dictionary<string, List<List<int>>> messages = [];
        await Task.WhenAll(
            poll.Offsets
            .Select(async kv => messages[kv.Key] = await GetLogs(kv.Key, kv.Value)));
        await node.ReplyAsync(message, new PollOk(messages));
    }

    [MaelstromHandler(CommitOffsets.CommitOffsetsType)]
    public async Task HandleCommitOffsets(Message message)
    {
        var commitOffsets = message.DeserializeAs<CommitOffsets>().Body;
        logger.LogInformation("Received commit offsets request: {Offsets}", commitOffsets.Offsets);
        await Task.WhenAll(
            commitOffsets.Offsets
            .Select(kv => UpdateCommittedOffset(kv.Key, kv.Value)));

        await node.ReplyAsync(message, new CommitOffsetsOk());
    }

    [MaelstromHandler(ListCommittedOffsets.ListCommittedOffsetsType)]
    public async Task HandleListCommittedOffsets(Message message)
    {
        var listCommittedOffsets = message.DeserializeAs<ListCommittedOffsets>().Body;
        logger.LogInformation("Received list committed offsets request: {Keys}", listCommittedOffsets.Keys);
        Dictionary<string, int> committedOffsets = (await Task.WhenAll(
            listCommittedOffsets.Keys
                .Select(async k => new KeyValuePair<string, int>(k, await GetCommittedOffset(k)))))
            .ToDictionary();

        await node.ReplyAsync(message, new ListCommittedOffsetsOk(committedOffsets));
    }

    private static string GetOffsetKey(string key) => $"offsets/{key}";

    private async Task<int> GetLatestOffset(string key) => await GetCounter(GetOffsetKey(key));

    private async Task<int> IncrementOffset(string key)
    {
        await _offsetLock.WaitAsync();
        try
        {
            return await IncrementCounter(GetOffsetKey(key));
        }
        finally
        {
            _offsetLock.Release();
        }
    }

    private static string GetCommittedKey(string key) => $"committed/{key}";

    private async Task<int> GetCommittedOffset(string key) => await GetCounter(GetCommittedKey(key));

    private async Task UpdateCommittedOffset(string key, int value)
    {
        string committedKey = GetCommittedKey(key);
        int attempts = 1;
        while (attempts <= _maxAttempts)
        {
            logger.LogDebug("Get counter {key}, attempt {attempts}", committedKey, attempts);
            var offset = await GetCounter(committedKey);
            if (value <= offset)
            {
                logger.LogDebug("New offset {value} is not greater than current offset {offset}, skipping", value, offset);
                return;
            }

            try
            {
                await node.LinKvStoreClient.CasAsync(committedKey, offset, value, createIfNotExists: true);
            }
            catch (KvStoreCasPreconditionFailed)
            {
                logger.LogWarning("CAS failed, waiting and retrying");
                await Task.Delay(10 + new Random().Next(-2, 2));
                attempts++;
                continue;
            }

            logger.LogDebug("Increment succeeded, new {key} = {offset}", key, committedKey);
            return;
        }

        logger.LogError("Increment offset failed after {attempts} attempts", _maxAttempts);
        throw new Exception("Increment offset failed after max attempts");
    }

    private async Task<int> GetCounter(string key) => await node.LinKvStoreClient.ReadOrDefaultAsync(key, 0);

    private async Task<int> IncrementCounter(string key) => await node.LinKvStoreClient.SafeUpdateAsync(key, v => v + 1, 0, maxAttempts: _maxAttempts);

    private static string GetLogKey(string key, int offset) => $"logs/{key}/{offset}";

    private async Task WriteLog(string key, int offset, int message)
    {
        logger.LogDebug("Writing log: {Key} {Offset} {Message}", key, offset, message);
        await node.SeqKvStoreClient.WriteAsync(GetLogKey(key, offset), message);
    }

    private async Task<List<List<int>>> GetLogs(string key, int offset)
    {
        var maxOffset = await GetLatestOffset(key);
        var logs = new List<List<int>>();
        while (logs.Count < _maxReturnedMessages && offset <= maxOffset)
        {
            try
            {
                var log = await node.SeqKvStoreClient.ReadAsync<string, int>(GetLogKey(key, offset));
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