using KafkaService;
using KafkaService.Models.MessageBodies;
using Maelstrom.Harness.InMemory;
using Maelstrom.TestSupport;

namespace KafkaServiceTests;

public class KafkaServiceTests : InMemoryHarnessTests
{
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

    [Fact]
    public async Task TestSendThenCommit()
    {
        var sendResult = await RpcAsync<Send, SendOk>(new Send
        {
            Key = "k1",
            Message = 123
        });
        Assert.True(sendResult.IsSuccess);
        var offset = sendResult.Result.Offset;

        var commitResult = await RpcAsync<CommitOffsets, CommitOffsetsOk>(new CommitOffsets
        {
            Offsets = new Dictionary<string, int>
            {
                { "k1", offset }
            }
        });
        Assert.True(commitResult.IsSuccess);

        var listCommittedResult = await RpcAsync<ListCommittedOffsets, ListCommittedOffsetsOk>(new ListCommittedOffsets
        {
            Keys = ["k1"]
        });
        Assert.True(listCommittedResult.IsSuccess);
        Assert.Equivalent(
            new Dictionary<string, int>
            {
                { "k1", offset }
            },
            listCommittedResult.Result.Offsets);
    }

    protected override InMemoryHarness SetupHarness(InMemoryHarness harness) => harness.AddWorkload<KafkaLog>();
}