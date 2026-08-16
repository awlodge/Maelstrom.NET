using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Maelstrom.Internals;

internal class MaelstromNode(ILogger<MaelstromNode> logger, IReceiver receiver, ISender sender) : IMaelstromNode, IDisposable
{
    private readonly ILogger<MaelstromNode> logger = logger;
    private readonly IReceiver _receiver = receiver;
    private readonly ISender _sender = sender;
    private string _nodeId = "";
    private string[] _nodeIds = [];

    private int _msgId = 0;
    private readonly ConcurrentDictionary<string, MaelstromHandler> _messageHandlers = [];
    private readonly ConcurrentDictionary<int, TaskCompletionSource<Message>> _replyHandlers = [];
    private readonly SemaphoreSlim _sendLock = new(1);

    public string NodeId => _nodeId;
    public string[] NodeIds => _nodeIds;

    internal delegate Task MaelstromHandler(Message msg, CancellationToken cancellationToken = default);

    internal void AddMessageHandlers(IDictionary<string, MaelstromHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            if (!_messageHandlers.TryAdd(handler.Key, handler.Value))
            {
                throw new InvalidOperationException($"Handler for message type {handler.Key} already registered");
            }
            logger.LogInformation("Registered handler for message type '{MessageType}'", handler.Key);
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
            activeHandlers.RemoveWhere(t => t.IsCompleted);
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
                    await this.ErrorAsync(message, ErrorCodes.Crash, $"Unexpected error handling message: {ex}", cancellationToken);
                }
            }
            else
            {
                logger.LogError("Message type {MessageType} not supported", message.Body.Type);
                await this.ErrorAsync(message, ErrorCodes.NotSupported, $"Message type {message.Body.Type} not supported", cancellationToken);
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
        if (message.Body.Type != Init.InitType)
        {
            await this.ErrorAsync(message, ErrorCodes.MalformedRequest, "First message must be an init message", cancellationToken);
            throw new Exception("First message must be an init message");
        }
        var init = message.DeserializeAs<Init>().Body;
        _nodeId = init.NodeId;
        _nodeIds = init.NodeIds;
        logger.LogInformation("Node initialized. Node ID: {NodeId}", NodeId);
        await this.ReplyAsync(message, new InitOk(), cancellationToken);
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

    public async Task SendAsync<T>(string destination, T body, CancellationToken cancellationToken = default) where T : MessageBody
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            body.MsgId = _msgId;
            var message = new Message<T>(NodeId, destination, body);
            var rawMessage = message.Serialize();
            logger.LogDebug("Sending message: {RawMessage}", rawMessage);
            await _sender.SendAsync(rawMessage, cancellationToken);
            _msgId++;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task<Message> RpcAsync<T>(string destination, T body, TimeSpan? timeout = null, CancellationToken cancellationToken = default) where T : MessageBody
    {
        Task<Message> replyTask;
        await _sendLock.WaitAsync(cancellationToken);
        var rpcMsgId = _msgId;
        try
        {
            body.MsgId = rpcMsgId;
            var message = new Message<T>(NodeId, destination, body);
            var rawMessage = message.Serialize();
            replyTask = AddReplyHander(rpcMsgId).Task;
            logger.LogDebug("Sending RPC message: {RawMessage}", rawMessage);
            await _sender.SendAsync(rawMessage, cancellationToken);
            _msgId++;
        }
        finally
        {
            _sendLock.Release();
        }

        var cancellationTask = Task.Delay(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
        await Task.WhenAny([replyTask, cancellationTask]);
        if (replyTask.IsCompletedSuccessfully)
        {
            return replyTask.Result;
        }
        else if (replyTask.IsFaulted)
        {
            throw new RpcFailedException("RPC failed", replyTask.Exception);
        }
        else if (cancellationTask.IsFaulted)
        {
            TryGetReplyHandler(rpcMsgId, out _);
            throw new RpcFailedException("RPC failed", cancellationTask.Exception);
        }
        else
        {
            TryGetReplyHandler(rpcMsgId, out _);
            throw new RpcFailedException("RPC timed out or was cancelled");
        }
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

public class RpcFailedException : Exception
{
    public RpcFailedException() : base() { }

    public RpcFailedException(string message) : base(message) { }

    public RpcFailedException(string message, Exception inner) : base(message, inner) { }
}