
using Maelstrom;
using Maelstrom.Harness.InMemory;
using Maelstrom.Models;
using Maelstrom.TestSupport;

namespace MaelstromHarnessTests;

public class HarnessTests : IAsyncLifetime
{
    private static readonly TimeSpan _defaultTImeout = TimeSpan.FromSeconds(1);
    private readonly InMemoryTestHarness _testHarness = new();

    private InMemoryHarness Harness => _testHarness.Harness;

    public async Task InitializeAsync()
    {
        await _testHarness.Harness.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _testHarness.DisposeAsync();
    }

    [Fact]
    public async Task WhenMessageSentToBadDestinationThenErrorReturned()
    {
        var result = await Harness.Client.RpcAsync<TestMessage, TestMessage>("bad_dest", new(), timeout: _defaultTImeout);
        Assert.True(result.IsError(out var error));
        Assert.Equal(ErrorCodes.NodeNotFound, error.ErrorCode);
    }

    [Fact]
    public async Task TestRoundTrip()
    {
        var messageReceived = false;
        Harness.Client.AddMessageHandler("test", async (Message message, CancellationToken ct) =>
        {
            var testMessage = message.DeserializeAs<TestMessage>();
            messageReceived = true;
            await Harness.Client.ReplyAsync(message, new TestMessage(), ct);
        });

        var result = await Harness.Client.RpcAsync<TestMessage, TestMessage>(Harness.Client.NodeId, new(), timeout: _defaultTImeout);
        Assert.True(result.IsSuccess);
        Assert.True(messageReceived);
    }

    [MessageType("test")]
    private class TestMessage : MessageBody
    {
    }
}