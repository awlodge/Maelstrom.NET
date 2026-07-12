using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Threading.Channels;

namespace Maelstrom.TestSupport;

public class MaelstromTestClient<TWorkload> : IAsyncDisposable where TWorkload : Workload
{
    private readonly Channel<string> _nodeInput = Channel.CreateUnbounded<string>();
    private readonly Channel<string> _nodeOutput = Channel.CreateUnbounded<string>();
    private readonly IHost _host;
    private Task? _runner = null;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private readonly string _srcNodeId = "node-0";
    private readonly string _dstNodeId = "node-1";
    private int _msgId = 1;

    public TimeSpan DefaultReceiveTimeOut { get; init; } = TimeSpan.FromSeconds(1);

    public MaelstromTestClient()
    {
        var receiver = new ChannelReceiver(_nodeInput);
        var sender = new ChannelSender(_nodeOutput);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IReceiver>(receiver);
        builder.Services.AddSingleton<ISender>(sender);
        builder.Services.AddMaelstromNodeWorkload<TWorkload, ChannelReceiver, ChannelSender>();

        _host = builder.Build();
    }

    public async Task SendAsync<T>(T body) where T : MessageBody
    {
        body.MsgId = _msgId;
        _msgId++;
        var message = new Message<T>(_srcNodeId, _dstNodeId, body);
        var rawMessage = message.Serialize();
        await _nodeInput.Writer.WriteAsync(rawMessage);
    }

    public async Task<Message<T>> ReadOutputAsync<T>(TimeSpan timeout = default) where T : MessageBody
    {
        if (timeout == default)
        {
            timeout = DefaultReceiveTimeOut;
        }
        var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(timeout);
        var rawMessage = await _nodeOutput.Reader.ReadAsync(cancellationSource.Token);
        var message = JsonSerializer.Deserialize<Message<MessageBody>>(rawMessage) ?? throw new InvalidOperationException($"Failed to deserialize: {rawMessage}");
        return message.DeserializeAs<T>();
    }

    public async Task StartAsync()
    {
        if (_runner is not null)
        {
            throw new InvalidOperationException("Already started");
        }

        _runner = _host.RunMaelstromNodeAsync(_cancellationTokenSource.Token);
        await SendInitAsync();
    }

    public async Task StopAsync()
    {
        if (_runner is null)
        {
            return;
        }
        _cancellationTokenSource.Cancel(true);
        await _runner;
        _runner = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _host.Dispose();
    }

    private async Task SendInitAsync()
    {
        var init = new Init
        {
            Type = Init.InitType,
            NodeId = _srcNodeId,
            NodeIds = []
        };
        await SendAsync(init);
        await ReadOutputAsync<InitOk>();
    }
}
