using EchoService;
using EchoService.Models.MessageBodies;
using Maelstrom.TestSupport;

namespace EchoServiceTests;

public class EchoServiceHarnessTests
{
    [Fact]
    public async Task TestEchoService()
    {
        await using var testHarness = new InMemoryTestHarness();
        var harness = await testHarness.Harness
            .AddWorkload<EchoServer>()
            .StartAsync();

        var result = await harness.Client.RpcAsync<Echo, EchoOk>(harness.WorkloadNodeIds.First(), new Echo { EchoMessage = "test" }, timeout: TimeSpan.FromSeconds(1));
        Assert.True(result.IsSuccess);
        Assert.Equal("test", result.Result.EchoMessage);
    }
}
