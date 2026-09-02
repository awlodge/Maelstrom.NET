using Maelstrom.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;

namespace Maelstrom.Harness.InMemory;

internal abstract class InMemoryNodeRunner : INode
{
    private readonly Channel<string> _inputChannel = Channel.CreateUnbounded<string>();
    private readonly Channel<string> _outputChannel = Channel.CreateUnbounded<string>();
    protected readonly IHost _host;
    private Task? _runner;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    internal InMemoryNodeRunner(Action<IHostApplicationBuilder>? configure = null)
    {
        Input = new ChannelSender(_inputChannel);
        Output = new ChannelReceiver(_outputChannel);

        var receiver = new ChannelReceiver(_inputChannel);
        var sender = new ChannelSender(_outputChannel);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IReceiver>(receiver);
        builder.Services.AddSingleton<ISender>(sender);
        configure?.Invoke(builder);

        _host = builder.Build();
    }

    public ISender Input { get; }
    public IReceiver Output { get; }

    public void Start()
    {
        if (_runner is not null)
        {
            throw new InvalidOperationException("Already started");
        }

        _runner = RunAsync(_cancellationTokenSource.Token);
    }

    protected abstract Task RunAsync(CancellationToken cancellationToken);

    public async Task StopAsync()
    {
        if (_runner is null)
        {
            return;
        }
        _cancellationTokenSource.Cancel(true);
        try
        {
            await _runner;
        }
        catch (OperationCanceledException)
        {
        }

        _runner = null;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await StopAsync();
        _host.Dispose();
    }
}

internal class InMemoryClientRunner : InMemoryNodeRunner
{
    private readonly MaelstromClient _client;

    internal InMemoryClientRunner(string nodeId)
        : base(b => b.Services.SetupMaelstromClientDependencies<ChannelReceiver, ChannelSender>())
    {
        var clientFactory = _host.Services.GetRequiredService<IMaelstromClientFactory>();
        _client = clientFactory.CreateMaelstromClient(nodeId);
    }

    internal IMaelstromClient Client => _client;

    internal void AddMessageHandlers(IEnumerable<(MaelstromHandlerAttribute, Delegate)> handlers)
        => _client.AddHandlers(handlers);

    protected override Task RunAsync(CancellationToken cancellationToken) => _client.RunAsync(cancellationToken);
}

internal class InMemoryWorkloadRunner<TWorkload> : InMemoryNodeRunner where TWorkload : Workload
{
    public InMemoryWorkloadRunner(Action<IHostApplicationBuilder>? configure = null)
        : base(b =>
        {
            b.Services.AddMaelstromNodeWorkload<TWorkload>();
            configure?.Invoke(b);
        })
    {
    }

    protected override Task RunAsync(CancellationToken cancellationToken) => _host.RunMaelstromNodeAsync(cancellationToken);
}