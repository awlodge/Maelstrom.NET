using Maelstrom.Harness.InMemory;
using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;
using Microsoft.Extensions.Hosting;

namespace Maelstrom.TestSupport;

public class MaelstromTestClient<TWorkload> : IAsyncDisposable, IMaelstromTestClient where TWorkload : Workload
{
    private readonly InMemoryNodeRunner _nodeRunner;
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
        _nodeRunner = new InMemoryWorkloadRunner<TWorkload>(configure);
    }

    public async Task SendAsync<T>(T body, CancellationToken cancellationToken = default) where T : MessageBody => await SendAsync(DstNodeId, body, cancellationToken);

    public async Task SendAsync<T>(string destination, T body, CancellationToken cancellationToken = default) where T : MessageBody => await SendAsync(body, NodeId, destination, cancellationToken);

    public async Task SendAsync<T>(T body, string src, string dst, CancellationToken cancellationToken = default) where T : MessageBody
    {
        body.MsgId = _msgId;
        _msgId++;
        var message = new Message<T>(src, dst, body);
        var rawMessage = message.Serialize();
        await _nodeRunner.Input.SendAsync(rawMessage, cancellationToken);
    }

    public async Task<Message> RecvAsync(TimeSpan timeout = default)
    {
        if (timeout == default)
        {
            timeout = DefaultReceiveTimeOut;
        }
        var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(timeout);
        var rawMessage = await _nodeRunner.Output.RecvAsync(cancellationSource.Token);
        return Message.Deserialize(rawMessage!) ?? throw new InvalidOperationException($"Failed to deserialize: {rawMessage}");
    }

    public async Task<RpcResult<TRecv>> RpcAsync<TSend, TRecv>(string destination, TSend body, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        where TSend : MessageBody
        where TRecv : MessageBody
    {
        await SendAsync(destination, body, cancellationToken);
        var response = await RecvAsync(timeout ?? default);
        return new RpcResult<TRecv>(response);
    }

    public Task<RpcResult<TRecv>> RpcAsync<TSend, TRecv>(TSend body, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        where TSend : MessageBody
        where TRecv : MessageBody
        => RpcAsync<TSend, TRecv>(DstNodeId, body, timeout, cancellationToken);


    public async Task<Message<T>> ReadOutputAsync<T>(TimeSpan timeout = default) where T : MessageBody
    {
        var message = await RecvAsync(timeout);
        return message.DeserializeAs<T>();
    }

    public async Task StartAsync()
    {
        _nodeRunner.Start();
        await SendInitAsync();
    }

    public async Task StopAsync()
    {
        await _nodeRunner.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await StopAsync();
        await _nodeRunner.DisposeAsync();
    }

    private async Task SendInitAsync()
    {
        var init = new Init(DstNodeId, []);
        await RpcAsync<Init, InitOk>(DstNodeId, init);
    }

    public void AddMessageHandler(string messageType, MaelstromHandlerAttribute.MaelstromHandler handler)
    {
        throw new NotImplementedException();
    }
}
