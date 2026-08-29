using EchoService;
using EchoService.Models.MessageBodies;
using Maelstrom.TestSupport;

namespace EchoServiceTests;

public class EchoServiceTests
{
    [Fact]
    public async Task TestEchoService()
    {
        await using var client = new MaelstromTestClient<EchoServer>();
        await client.StartAsync();
        var echo = new Echo
        {
            EchoMessage = "ping"
        };
        var result = await client.RpcAsync<Echo, EchoOk>(echo);
        Assert.True(result.IsSuccess);
        Assert.Equal("ping", result.Result.EchoMessage);
    }
}