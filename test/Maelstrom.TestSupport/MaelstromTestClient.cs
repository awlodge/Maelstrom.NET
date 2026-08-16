using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Threading.Channels;

namespace Maelstrom.TestSupport;

public class MaelstromTestClient<TWorkload> : IAsyncDisposable, IMaelstromTestClient where TWorkload : Workload
{
    private readonly Channel<string> _nodeInput = Channel.CreateUnbounded<string>();
    private readonly Channel<string> _nodeOutput = Channel.CreateUnbounded<string>();
    private readonly IHost _host;
    private Task? _runner = null;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly KvStore _kvStore;

    private const string _srcNodeId = "c1";
    private const string _dstNodeId = "n1";
    private int _msgId = 1;

    public TimeSpan DefaultReceiveTimeOut { get; init; } = TimeSpan.FromSeconds(1);

    public string NodeId => _srcNodeId;
    public string DstNodeId => _dstNodeId;

    public IKvStore KvStore => _kvStore;

    public MaelstromTestClient(Action<IHostApplicationBuilder>? configure = null)
    {
        _kvStore = new KvStore(this);
        var receiver = new ChannelReceiver(_nodeInput);
        var sender = new ChannelSender(_nodeOutput);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IReceiver>(receiver);
        builder.Services.AddSingleton<ISender>(sender);
        builder.Services.AddMaelstromNodeWorkload<TWorkload, ChannelReceiver, ChannelSender>();
        configure?.Invoke(builder);

        _host = builder.Build();
    }

    public async Task SendAsync<T>(T body) where T : MessageBody => await SendAsync(DstNodeId, body);

    public async Task SendAsync<T>(string destination, T body, CancellationToken cancellationToken = default) where T : MessageBody => await SendAsync(body, NodeId, destination);

    public async Task SendAsync<T>(T body, string src, string dst) where T : MessageBody
    {
        body.MsgId = _msgId;
        _msgId++;
        var message = new Message<T>(src, dst, body);
        var rawMessage = message.Serialize();
        await _nodeInput.Writer.WriteAsync(rawMessage);
    }

    public async Task<Message> RecvAsync(TimeSpan timeout = default)
    {
        if (timeout == default)
        {
            timeout = DefaultReceiveTimeOut;
        }
        var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(timeout);
        var rawMessage = await _nodeOutput.Reader.ReadAsync(cancellationSource.Token);
        return JsonSerializer.Deserialize<Message<MessageBody>>(rawMessage) ?? throw new InvalidOperationException($"Failed to deserialize: {rawMessage}");
    }

    public async Task<Message> RpcAsync<T>(string destination, T body, TimeSpan? timeout = null, CancellationToken cancellationToken = default) where T : MessageBody
    {
        await SendAsync(destination, body, cancellationToken);
        return await RecvAsync(timeout ?? default);
    }

    public async Task<Message<T>> ReadOutputAsync<T>(TimeSpan timeout = default) where T : MessageBody
    {
        var message = await RecvAsync(timeout);
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
        GC.SuppressFinalize(this);
        await StopAsync();
        _host.Dispose();
    }

    private async Task SendInitAsync()
    {
        var init = new Init
        {
            Type = Init.InitType,
            NodeId = DstNodeId,
            NodeIds = []
        };
        await RpcAsync(DstNodeId, init);
    }
}
