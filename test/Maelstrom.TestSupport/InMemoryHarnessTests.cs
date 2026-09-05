using Maelstrom.Harness.InMemory;
using Maelstrom.Models;
using Xunit;

namespace Maelstrom.TestSupport;

public abstract class InMemoryHarnessTests(TimeSpan? defaultTimeout = null) : IAsyncLifetime
{
    private readonly TimeSpan _defaultTImeout = defaultTimeout ?? TimeSpan.FromSeconds(1);
    private readonly InMemoryTestHarness _testHarness = new();

    protected IMaelstromClient Client => _testHarness.Harness.Client;
    protected string DefaultDstNodeId => _testHarness.Harness.WorkloadNodeIds.First();

    public async Task InitializeAsync()
    {
        await SetupHarness(_testHarness.Harness).StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _testHarness.DisposeAsync();
    }

    protected abstract InMemoryHarness SetupHarness(InMemoryHarness harness);

    protected Task<RpcResult<TRec>> RpcAsync<TSend, TRec>(TSend body)
        where TSend : MessageBody
        where TRec : MessageBody => Client.RpcAsync<TSend, TRec>(DefaultDstNodeId, body, timeout: _defaultTImeout);
}
