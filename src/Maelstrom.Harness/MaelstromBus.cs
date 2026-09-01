using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Maelstrom.Harness;

public class MaelstromBus(ILogger<MaelstromBus> logger) : IAsyncDisposable
{
    private delegate Task ProcessMessage(Node node, string msg, CancellationToken cancellationToken);

    private readonly ILogger<MaelstromBus> _logger = logger;
    private readonly ConcurrentDictionary<string, Node> _nodes = [];
    private bool _started = false;

    public void Start()
    {
        _started = true;
        foreach (var node in _nodes.Values)
        {
            StartNode(node);
        }
    }

    public async Task StopAsync()
    {
        if (!_started)
        {
            return;
        }

        _logger.LogInformation("Stopping...");
        await Task.WhenAll(_nodes.Values.Select(StopNode));
        _logger.LogInformation("Stopped");
        _started = false;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await StopAsync();
        await Task.WhenAll(_nodes.Values.Select(async n => await n.DisposeAsync()));
        _nodes.Clear();
    }

    public bool AddNode(string nodeId, IReceiver output, ISender input)
    {
        var node = new Node(nodeId, output, input);
        if (!_nodes.TryAdd(nodeId, node))
        {
            _logger.LogWarning("Node {NodeId} already added", nodeId);
            return false;
        }

        _logger.LogInformation("Added node {NodeId}", nodeId);
        if (_started)
        {
            StartNode(node);
        }
        return true;
    }

    public async Task RemoveNodeAsync(string nodeId)
    {
        if (!_nodes.TryRemove(nodeId, out var node))
        {
            _logger.LogInformation("Node {NodeId} already removed", nodeId);
            return;
        }

        await StopNode(node);
        await node.DisposeAsync();
    }

    private void StartNode(Node node)
    {
        if (!_started)
        {
            throw new InvalidOperationException("Message Bus not started");
        }

        _logger.LogInformation("Start node {NodeId}", node.NodeId);
        node.Start(ProcessMessageAsync);
    }

    private async Task StopNode(Node node)
    {
        _logger.LogInformation("Stopping node {NodeId}", node.NodeId);
        await node.StopAsync();
        _logger.LogInformation("Stopped node {NodeId}", node.NodeId);
    }

    private async Task ProcessMessageAsync(Node node, string msg, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received message on node {NodeId}", node.NodeId);
        Message? message;
        try
        {
            message = Message.Deserialize(msg);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing message");
            await node.ErrorAsync("BUS", ErrorCodes.MalformedRequest, "Failed to deserialize message", null, cancellationToken);
            return;
        }

        if (!_nodes.TryGetValue(message!.Dest, out var destNode))
        {
            _logger.LogError("Cannot find destination node {DstNodeId}", message.Dest);
            await node.ErrorAsync("BUS", ErrorCodes.NodeNotFound, $"Destination {message.Dest} not found", message.Body.MsgId, cancellationToken);
            return;
        }

        _logger.LogInformation("Send {SrcNodeId} => {DstNodeId}", message.Src, message.Dest);
        await destNode.SendAsync(msg, cancellationToken);
    }

    private class Node(string nodeId, IReceiver output, ISender input) : IAsyncDisposable
    {
        private readonly IReceiver _output = output;
        private readonly ISender _input = input;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _loop;

        public string NodeId => nodeId;

        public Task SendAsync(string message, CancellationToken cancellationToken) =>
            _input.SendAsync(message, cancellationToken);

        public async Task ErrorAsync(string src, ErrorCodes errorCode, string errorMessage, int? inReplyTo, CancellationToken cancellationToken)
        {
            var body = new ErrorBody(errorCode, errorMessage);
            body.InReplyTo = inReplyTo;
            var message = new Message<ErrorBody>(src, NodeId, body);
            await SendAsync(message.Serialize(), cancellationToken);
        }

        public void Start(ProcessMessage processMessage)
        {
            if (_loop is not null)
            {
                throw new InvalidOperationException($"Node {NodeId} is already running");
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _loop = RunAsync(processMessage, _cancellationTokenSource.Token);
        }

        public async Task StopAsync()
        {
            if (_loop is null)
            {
                return;
            }

            _cancellationTokenSource?.Cancel();
            try
            {
                await _loop;
            }
            catch (OperationCanceledException)
            {
            }

            _loop = null;
            _cancellationTokenSource = null;
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }

        private async Task RunAsync(ProcessMessage processMessage, CancellationToken cancellationToken)
        {
            HashSet<Task> activeHandlers = [];
            while (!cancellationToken.IsCancellationRequested)
            {
                var msg = await _output.RecvAsync(cancellationToken);
                if (msg != null)
                {
                    activeHandlers.Add(processMessage(this, msg, cancellationToken));
                }
                activeHandlers.RemoveWhere(t => t.IsCompleted);
            }

            await Task.WhenAll(activeHandlers);
        }
    }
}
