using BroadcastService.Models.MessageBodies;
using Maelstrom.TestSupport;

namespace BroadcastServiceTests
{
    public class BroadcastServiceTests
    {
        [Fact]
        public async Task TestBroadcastThenRead()
        {
            await using var client = new MaelstromTestClient<BroadcastService.BroadcastService>();
            await client.StartAsync();

            var broadcast = new Broadcast(1);
            await client.SendAsync(broadcast);
            var response = await client.ReadOutputAsync<BroadcastOk>();
            Assert.NotNull(response);

            var read = new Read();
            await client.SendAsync(read);
            var readResponse = await client.ReadOutputAsync<ReadOk>();
            Assert.NotNull(readResponse);
            Assert.Equal([1], readResponse.Body.ReadMessages);
        }

        [Fact]
        public async Task TestBroadcastToTopologyNeighbors()
        {
            await using var client = new MaelstromTestClient<BroadcastService.BroadcastService>();
            await client.StartAsync();

            var topology = new Topology
            {
                Type = Topology.TopologyType,
                TopologyData = new Dictionary<string, string[]>
                {
                    { client.DstNodeId, ["b1", "b2"] }
                }
            };
            await client.SendAsync(topology);
            var response = await client.ReadOutputAsync<TopologyOk>();
            Assert.NotNull(response);

            var broadcast = new Broadcast(1);
            await client.SendAsync(broadcast);
            var response2 = await client.ReadOutputAsync<BroadcastOk>();
            Assert.NotNull(response2);

            var broadcastRequest1 = await client.ReadOutputAsync<Broadcast>();
            Assert.NotNull(broadcastRequest1);
            Assert.Equal("b1", broadcastRequest1.Dest);
            await client.SendAsync(GetBroadcastResponse(broadcastRequest1.Body), "b1", client.DstNodeId);


            var broadcastRequest2 = await client.ReadOutputAsync<Broadcast>();
            Assert.NotNull(broadcastRequest2);
            Assert.Equal("b2", broadcastRequest2.Dest);
            await client.SendAsync(GetBroadcastResponse(broadcastRequest2.Body), "b2", client.DstNodeId);

            var read = new Read();
            await client.SendAsync(read);
            var readResponse = await client.ReadOutputAsync<ReadOk>();
            Assert.NotNull(readResponse);
            Assert.Equal([1], readResponse.Body.ReadMessages);
        }

        private static BroadcastOk GetBroadcastResponse(Broadcast request) => new BroadcastOk()
        {
            InReplyTo = request.MsgId
        };
    }
}