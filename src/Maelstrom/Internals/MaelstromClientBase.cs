using Maelstrom.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Maelstrom.Internals;

internal abstract class MaelstromClientBase(ILogger logger, IReceiver receiver, ISender sender) : IMaelstromClient, IDisposable
{
    private readonly ILogger logger = logger;
    private readonly IReceiver _receiver = receiver;
    private readonly ISender _sender = sender;

    private volatile int _msgId = 0;
    private readonly ConcurrentDictionary<string, MessageHandler> _messageHandlers = [];
    private readonly ConcurrentDictionary<int, TaskCompletionSource<Message>> _replyHandlers = [];

    public abstract string NodeId { get; }

    public void AddMessageHandler<T>(MaelstromHandlerAttribute.MaelstromHandler<T> handler)
        where T : MessageBody
    {
        var messageHandler = new MessageHandler<T>(handler);
        if (!_messageHandlers.TryAdd(messageHandler.MessageType, messageHandler))
        {
            throw new InvalidOperationException($"Handler for message type '{messageHandler.MessageType}' already registered");
        }
        logger.LogInformation("Registered handler for message type '{MessageType}'", messageHandler.MessageType);
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting...");
        await InitAsync(stoppingToken);
        HashSet<Task> activeHandlers = [];
        try
        {
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
        }
        finally
        {
            logger.LogInformation("Waiting for active tasks to complete...");
            await Task.WhenAll(activeHandlers);
        }

        logger.LogInformation("Stopped");
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
                    await handler.HandleAsync(message, cancellationToken);
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

    protected virtual Task InitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose()
    {
        _sender.Dispose();
        _receiver.Dispose();
    }

    protected async Task<Message?> RecvAsync(CancellationToken? cancellationToken = null)
    {
        var rawMessage = await _receiver.RecvAsync(cancellationToken ?? CancellationToken.None);
        logger.LogDebug("Received message: {RawMessage}", rawMessage);
        if (rawMessage == null)
        {
            return null;
        }

        try
        {
            return Message.Deserialize(rawMessage);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Error deserializing message");
            return null;
        }
    }

    public async Task SendAsync<T>(string destination, T body, CancellationToken cancellationToken = default) where T : MessageBody
    {
        body.MsgId = GetMessageId();
        var message = new Message<T>(NodeId, destination, body);
        var rawMessage = message.Serialize();
        logger.LogDebug("Sending message: {RawMessage}", rawMessage);
        await _sender.SendAsync(rawMessage, cancellationToken);
    }

    public async Task<RpcResult<TRecv>> RpcAsync<TSend, TRecv>(string destination, TSend body, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        where TSend : MessageBody
        where TRecv : MessageBody
    {
        Task<Message> replyTask;
        var rpcMsgId = GetMessageId();
        body.MsgId = rpcMsgId;
        var message = new Message<TSend>(NodeId, destination, body);
        var rawMessage = message.Serialize();
        replyTask = AddReplyHander(rpcMsgId).Task;
        logger.LogDebug("Sending RPC message: {RawMessage}", rawMessage);
        await _sender.SendAsync(rawMessage, cancellationToken);

        var cancellationTask = Task.Delay(timeout ?? Timeout.InfiniteTimeSpan, cancellationToken);
        await Task.WhenAny([replyTask, cancellationTask]);
        if (replyTask.IsCompletedSuccessfully)
        {
            return new RpcResult<TRecv>(replyTask.Result);
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

    private int GetMessageId() => Interlocked.Increment(ref _msgId);

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

    private abstract class MessageHandler
    {
        public abstract string MessageType { get; }
        public abstract Task HandleAsync(Message message, CancellationToken cancellationToken);
    }

    private class MessageHandler<T> : MessageHandler where T : MessageBody
    {
        private readonly MaelstromHandlerAttribute.MaelstromHandler<T> _handler;
        public MessageHandler(MaelstromHandlerAttribute.MaelstromHandler<T> handler)
        {
            _handler = handler;
            MessageType = MessageTypeAttribute.GetMessageType<T>();
        }
        public override string MessageType { get; }

        public override Task HandleAsync(Message message, CancellationToken cancellationToken)
            => _handler(message.DeserializeAs<T>(), cancellationToken);
    }
}
