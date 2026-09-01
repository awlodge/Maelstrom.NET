using Maelstrom.Harness;
using Maelstrom.Harness.InMemory;
using Maelstrom.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Maelstrom.TestSupport;

public class InMemoryTestHarness : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly IKvStoreClientFactory _kvStoreClientFactory;

    public InMemoryTestHarness()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddInMemoryHarness();
        builder.Services.TryAddSingleton<IKvStoreClientFactory, KvStoreClientFactory>();
        _host = builder.Build();
        Harness = _host.GetInMemoryHarness();
        _kvStoreClientFactory = _host.Services.GetRequiredService<IKvStoreClientFactory>();
    }

    public InMemoryHarness Harness { get; }
    public IKvStoreClient SeqKvStore => _kvStoreClientFactory.Create("seq-kv", Harness.Client);
    public IKvStoreClient LinKvStore => _kvStoreClientFactory.Create("lin-kv", Harness.Client);

    public async ValueTask DisposeAsync()
    {
        await Harness.DisposeAsync();
        _host.Dispose();
    }
}
