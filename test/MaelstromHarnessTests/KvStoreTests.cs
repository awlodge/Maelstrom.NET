using Maelstrom;
using Maelstrom.Harness.InMemory;
using Maelstrom.Internals;
using Maelstrom.TestSupport;

namespace MaelstromHarnessTests;

public abstract class KvStoreTests(string nodeId) : IAsyncLifetime
{
    private static readonly TimeSpan _defaultTImeout = TimeSpan.FromSeconds(1);
    private readonly InMemoryTestHarness _testHarness = new();

    private InMemoryHarness Harness => _testHarness.Harness;
    private IKvStoreClient KvStoreClient => nodeId switch
    {
        "seq-kv" => _testHarness.SeqKvStore,
        "lin-kv" => _testHarness.LinKvStore,
        _ => throw new ArgumentOutOfRangeException(nameof(nodeId), "Unknown KV store type")
    };

    public async Task InitializeAsync()
    {
        await _testHarness.Harness.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _testHarness.DisposeAsync();
    }

    [Fact]
    public async Task TestReadNotFound()
    {
        await Assert.ThrowsAsync<KvStoreKeyNotFoundException>(async () =>
        {
            await KvStoreClient.ReadAsync<string, string>("test");
        });
    }

    [Theory]
    [InlineData("test")]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(1)]
    [InlineData(4.5)]
    public async Task TestWriteThenRead(object key)
    {
        await KvStoreClient.WriteAsync(key, "hello");
        var val = await KvStoreClient.ReadAsync<object, string>(key);
        Assert.Equal("hello", val);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TestSuccessfulCas(bool createIfNotExists)
    {
        await KvStoreClient.WriteAsync("test", 0);
        var val = await KvStoreClient.ReadAsync<string, int>("test");
        Assert.Equal(0, val);

        await KvStoreClient.CasAsync("test", 0, 1, createIfNotExists: createIfNotExists);
        var val2 = await KvStoreClient.ReadAsync<string, int>("test");
        Assert.Equal(1, val2);
    }

    [Fact]
    public async Task TestCasCreateIfNotExists()
    {
        await KvStoreClient.CasAsync("test", 0, 1, createIfNotExists: true);
        var val = await KvStoreClient.ReadAsync<string, int>("test");
        Assert.Equal(1, val);
    }

    [Fact]
    public async Task TestCasWhenCreateIfNotExistsFalse()
    {
        await Assert.ThrowsAsync<KvStoreKeyNotFoundException>(async () =>
        {
            await KvStoreClient.CasAsync("test", 0, 1, createIfNotExists: false);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TestUnsuccessfulCas(bool createIfNotExists)
    {
        await KvStoreClient.WriteAsync("test", 0);
        var val = await KvStoreClient.ReadAsync<string, int>("test");
        Assert.Equal(0, val);

        await Assert.ThrowsAsync<KvStoreCasPreconditionFailed>(async () =>
        {
            await KvStoreClient.CasAsync("test", 1, 2, createIfNotExists);
        });

        var val2 = await KvStoreClient.ReadAsync<string, int>("test");
        Assert.Equal(0, val2);
    }

    [Fact]
    public async Task TestParallelCas()
    {
        await KvStoreClient.WriteAsync("test", 0);
        var val = await KvStoreClient.ReadAsync<string, int>("test");
        Assert.Equal(0, val);

        Task[] casAttempts = [
            KvStoreClient.CasAsync("test", 0, 1),
            KvStoreClient.CasAsync("test", 0, 1)
        ];

        await Assert.ThrowsAsync<KvStoreCasPreconditionFailed>(async () =>
        {
            await Task.WhenAll(casAttempts);
        });

        await Assert.Single(casAttempts.Where(t => t.IsCompletedSuccessfully));

        var val2 = await KvStoreClient.ReadAsync<string, int>("test");
        Assert.Equal(1, val2);
    }
}

public class SeqKvStoreTests() : KvStoreTests("seq-kv")
{
}

public class LinKvStoreTests() : KvStoreTests("lin-kv")
{
}