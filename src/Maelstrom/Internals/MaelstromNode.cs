using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Maelstrom;

internal class MaelstromNode : IMaelstromNode, IDisposable
{
    private readonly ILogger<MaelstromNode> logger;
    private readonly IReceiver _receiver;
    private readonly ISender _sender;
    private string _nodeId = "";
    private string[] _nodeIds = [];
    private readonly KvStoreClient _seqKvStoreClient;
    private readonly KvStoreClient _linKvStoreClient;

    private int _msgId = 0;
    private readonly ConcurrentDictionary<string, MaelstromHandler> _messageHandlers = [];
    private readonly ConcurrentDictionary<int, TaskCompletionSource<Message>> _replyHandlers = [];
    private readonly SemaphoreSlim _sendLock = new(1);

    public string NodeId => _nodeId;
    public string[] NodeIds => _nodeIds;
    public IKvStoreClient SeqKvStoreClient => _seqKvStoreClient;
    public IKvStoreClient LinKvStoreClient => _linKvStoreClient;

    internal delegate Task MaelstromHandler(Message msg, CancellationToken cancellationToken = default);

    public MaelstromNode(ILogger<MaelstromNode> logger, IReceiver receiver, ISender sender)
    {
        this.logger = logger;
        _receiver = receiver;
        _sender = sender;
        _seqKvStoreClient = new(this, this.logger, "seq-kv");
        _linKvStoreClient = new(this, this.logger, "lin-kv");
    }

    internal void AddMessageHandlers(IDictionary<string, MaelstromHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            if (!_messageHandlers.TryAdd(handler.Key, handler.Value))
            {
                throw new InvalidOperationException($"Handler for message type {handler.Key} already registered");
            }
        }
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting...");
        await InitAsync(stoppingToken);
        HashSet<Task> activeHandlers = [];
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await RecvAsync(stoppingToken);
            if (message != null)
            {
                logger.LogInformation("Received message of type: {MessageType}", message.Body.Type);
                activeHandlers.Add(ProcessMessageAsync(message, stoppingToken));
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        logger.LogInformation("Waiting for active tasks to complete...");
        await Task.WhenAll(activeHandlers);
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            if (message.Body.InReplyTo != null)
            {
                int replyId = message.Body.InReplyTo.Value;
                if (!TryGetReplyHandler(replyId, out var replyTcs))
                {
                    logger.LogError("No handler found for reply message with id {ReplyId}", replyId);
                }
                else
                {
                    replyTcs.SetResult(message);
                }
            }
            else if (_messageHandlers.TryGetValue(message.Body.Type, out var handler))
            {
                try
                {
                    await handler(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error handling message of type {messageType}", message.Body.Type);
                    await ErrorAsync(message, ErrorCodes.Crash, $"Unexpected error handling message: {ex}");
                }
            }
            else
            {
                logger.LogError("Message type {MessageType} not supported", message.Body.Type);
                await ErrorAsync(message, ErrorCodes.NotSupported, $"Message type {message.Body.Type} not supported");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing message");
        }
    }

    private async Task InitAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Awaiting init message");
        var message = await RecvAsync(cancellationToken);
        if (message == null || message.Body == null)
        {
            throw new Exception("Failed to receive init message");
        }
        if (message.Body.Type != "init")
        {
            await ErrorAsync(message, ErrorCodes.MalformedRequest, "First message must be an init message");
            throw new Exception("First message must be an init message");
        }
        var init = message.DeserializeAs<Init>().Body;
        _nodeId = init.NodeId;
        _nodeIds = init.NodeIds;
        logger.LogInformation("Node initialized. Node ID: {NodeId}", NodeId);
        await ReplyAsync(message, new InitOk());
    }

    public void Dispose()
    {
        _sender.Dispose();
        _receiver.Dispose();
    }

    private async Task<Message?> RecvAsync(CancellationToken? cancellationToken = null)
    {
        var rawMessage = await _receiver.RecvAsync(cancellationToken ?? CancellationToken.None);
        logger.LogDebug("Received message: {RawMessage}", rawMessage);
        if (rawMessage == null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Message<MessageBody>>(rawMessage);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error deserializing message");
            return null;
        }
    }

    public async Task SendAsync<T>(string destination, T body) where T : MessageBody
    {
        await _sendLock.WaitAsync();
        try
        {
            body.MsgId = _msgId;
            var message = new Message<T>(NodeId, destination, body);
            var rawMessage = message.Serialize();
            logger.LogDebug("Sending message: {RawMessage}", rawMessage);
            await _sender.SendAsync(rawMessage, CancellationToken.None);
            _msgId++;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task ReplyAsync(Message originalMessage, MessageBody body)
    {
        if (originalMessage.Body.MsgId == null)
        {
            throw new Exception("For reply, original message must have a MsgId");
        }
        body.InReplyTo = (int)originalMessage.Body.MsgId;
        await SendAsync(originalMessage.Src, body);
    }

    public async Task ErrorAsync(Message originalMessage, ErrorCodes errorCode, string errorMessage)
    {
        var body = new ErrorBody(errorCode, errorMessage);
        await ReplyAsync(originalMessage, body);
    }

    public async Task<Message> RpcAsync<T>(string destination, T body) where T : MessageBody
    {
        Task<Message> replyTask;
        await _sendLock.WaitAsync();
        try
        {
            body.MsgId = _msgId;
            var message = new Message<T>(NodeId, destination, body);
            var rawMessage = message.Serialize();
            replyTask = AddReplyHander(_msgId).Task;
            logger.LogDebug("Sending RPC message: {RawMessage}", rawMessage);
            await _sender.SendAsync(rawMessage, CancellationToken.None);
            _msgId++;
        }
        finally
        {
            _sendLock.Release();
        }

        return await replyTask;
    }

    private TaskCompletionSource<Message> AddReplyHander(int msgId)
    {
        var tcs = new TaskCompletionSource<Message>();
        if (!_replyHandlers.TryAdd(msgId, tcs))
        {
            throw new InvalidOperationException($"Reply handler already registered for message ID {msgId}");
        }

        return tcs;
    }

    private bool TryGetReplyHandler(int msgId, [NotNullWhen(true)] out TaskCompletionSource<Message>? tcs)
    {
        return _replyHandlers.TryRemove(msgId, out tcs);
    }
}
