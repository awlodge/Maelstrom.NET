using KafkaService;
using KafkaService.Models.MessageBodies;
using Maelstrom;
using Maelstrom.Models;
using Maelstrom.TestSupport;

namespace KafkaServiceTests;

public class KafkaServiceTests : IAsyncLifetime
{
    private static readonly TimeSpan _defaultTImeout = TimeSpan.FromSeconds(1);
    private readonly InMemoryTestHarness _testHarness = new();

    private IMaelstromClient Client => _testHarness.Harness.Client;
    private string DstNodeId => _testHarness.Harness.WorkloadNodeIds.First();

    public async Task InitializeAsync()
    {
        await _testHarness.Harness
            .AddWorkload<KafkaLog>()
            .StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _testHarness.DisposeAsync();
    }

    [Fact]
    public async Task TestSendThenPoll()
    {
        var sendResult = await RpcAsync<Send, SendOk>(new Send
        {
            Key = "k1",
            Message = 123
        });
        Assert.True(sendResult.IsSuccess);
        var offset = sendResult.Result.Offset;

        var pollResult = await RpcAsync<Poll, PollOk>(new Poll
        {
            Offsets = new Dictionary<string, int>
            {
                { "k1", offset }
            }
        });
        Assert.True(pollResult.IsSuccess);
        Assert.Equivalent(new List<string> { "k1" }, pollResult.Result.Messages.Keys);
        var messages = pollResult.Result.Messages["k1"];
        Assert.Single(messages);
        Assert.Equivalent(new List<int> { offset, 123 }, messages.First());
    }

    private Task<RpcResult<TRec>> RpcAsync<TSend, TRec>(TSend body)
        where TSend : MessageBody
        where TRec : MessageBody => Client.RpcAsync<TSend, TRec>(DstNodeId, body, timeout: _defaultTImeout);
}