using EchoService;
using EchoService.Models.MessageBodies;
using Maelstrom.Harness.InMemory;
using Maelstrom.TestSupport;

namespace EchoServiceTests;

public class EchoServiceHarnessTests : InMemoryHarnessTests
{
    [Fact]
    public async Task TestEchoService()
    {
        var result = await RpcAsync<Echo, EchoOk>(new Echo { EchoMessage = "test" });
        Assert.True(result.IsSuccess);
        Assert.Equal("test", result.Result.EchoMessage);
    }

    protected override InMemoryHarness SetupHarness(InMemoryHarness harness) => harness.AddWorkload<EchoServer>();
}
