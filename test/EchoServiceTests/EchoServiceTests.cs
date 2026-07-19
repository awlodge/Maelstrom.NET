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
            Type = Echo.EchoType,
            EchoMessage = "ping"
        };
        await client.SendAsync(echo);
        var response = await client.ReadOutputAsync<EchoOk>();
        Assert.NotNull(response);
        Assert.Equal("ping", response.Body.EchoMessage);
    }
}