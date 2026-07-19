using CounterService;
using CounterService.Models.MessageBodies;
using Maelstrom.TestSupport;

namespace CounterServiceTests;

public class CounterServiceTests : IAsyncLifetime
{
    private readonly MaelstromTestClient<Counter> _client;

    public CounterServiceTests()
    {
        _client = new MaelstromTestClient<Counter>();
    }

    public async Task InitializeAsync()
    {
        await _client.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _client.DisposeAsync();
    }

    [Fact]
    public async Task TestRead()
    {
        var read = new Read();
        await _client.SendAsync(read);

        var lookupResult = await _client.KvStore.ExpectReadAsync("seq-kv", "counter");
        Assert.False(lookupResult);

        var casResult = await _client.KvStore.ExpecCasAsync("seq-kv", "counter", 0, 0, true);
        Assert.True(casResult);

        var readResponse = await _client.ReadOutputAsync<ReadOk<int>>();
        Assert.NotNull(readResponse);
        Assert.Equal(0, readResponse.Body.Value);
    }

    [Fact]
    public async Task TestAddThenRead()
    {
        var add = new Add(1);
        await _client.SendAsync(add);

        var addResponse = await _client.ReadOutputAsync<AddOk>();
        Assert.NotNull(addResponse);
        Assert.Equal(AddOk.AddOkType, addResponse.Body.Type);

        var lookupResult = await _client.KvStore.ExpectReadAsync("seq-kv", "counter");
        Assert.False(lookupResult);

        var casResult = await _client.KvStore.ExpecCasAsync("seq-kv", "counter", 0, 1, true);
        Assert.True(casResult);

        var read = new Read();
        await _client.SendAsync(read);

        var lookupResult2 = await _client.KvStore.ExpectReadAsync("seq-kv", "counter");
        Assert.True(lookupResult2);

        var casResult2 = await _client.KvStore.ExpecCasAsync("seq-kv", "counter", 1, 1, true);
        Assert.True(casResult2);

        var readResponse = await _client.ReadOutputAsync<ReadOk<int>>();
        Assert.NotNull(readResponse);
        Assert.Equal(1, readResponse.Body.Value);
    }
}