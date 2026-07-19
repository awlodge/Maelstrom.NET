using BroadcastService;
using BroadcastService.Models.MessageBodies;
using Maelstrom.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace BroadcastServiceTests
{
    public class BroadcastServiceTests : IAsyncLifetime
    {
        private readonly MaelstromTestClient<BroadcastService.BroadcastService> _client;

        public BroadcastServiceTests()
        {
            _client = new MaelstromTestClient<BroadcastService.BroadcastService>(b =>
            {
                b.Services.Configure<BroadcastServiceOptions>(options =>
                {
                    // Reduce timeout/delay to minimize test execution time.
                    options.RpcTimeout = TimeSpan.FromMilliseconds(200);
                    options.RpcRetryDelay = TimeSpan.Zero;
                });
            });
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
        public async Task TestBroadcastThenRead()
        {
            var broadcast = new Broadcast(1);
            await _client.SendAsync(broadcast);
            var response = await _client.ReadOutputAsync<BroadcastOk>();
            Assert.NotNull(response);

            var read = new Read();
            await _client.SendAsync(read);
            var readResponse = await _client.ReadOutputAsync<ReadOk>();
            Assert.NotNull(readResponse);
            Assert.Equal([1], readResponse.Body.ReadMessages);
        }

        [Fact]
        public async Task TestBroadcastToTopologyNeighbors()
        {
            var topology = new Topology
            {
                Type = Topology.TopologyType,
                TopologyData = new Dictionary<string, string[]>
                {
                    { _client.DstNodeId, ["b1", "b2"] }
                }
            };
            await _client.SendAsync(topology);
            var response = await _client.ReadOutputAsync<TopologyOk>();
            Assert.NotNull(response);

            var broadcast = new Broadcast(1);
            await _client.SendAsync(broadcast);
            var response2 = await _client.ReadOutputAsync<BroadcastOk>();
            Assert.NotNull(response2);

            var broadcastRequest1 = await _client.ReadOutputAsync<Broadcast>();
            Assert.NotNull(broadcastRequest1);
            Assert.Equal("b1", broadcastRequest1.Dest);
            await _client.SendAsync(GetBroadcastResponse(broadcastRequest1.Body), "b1", _client.DstNodeId);


            var broadcastRequest2 = await _client.ReadOutputAsync<Broadcast>();
            Assert.NotNull(broadcastRequest2);
            Assert.Equal("b2", broadcastRequest2.Dest);
            await _client.SendAsync(GetBroadcastResponse(broadcastRequest2.Body), "b2", _client.DstNodeId);

            var read = new Read();
            await _client.SendAsync(read);
            var readResponse = await _client.ReadOutputAsync<ReadOk>();
            Assert.NotNull(readResponse);
            Assert.Equal([1], readResponse.Body.ReadMessages);
        }

        [Fact]
        public async Task TestBroadcastRetries()
        {
            var topology = new Topology
            {
                Type = Topology.TopologyType,
                TopologyData = new Dictionary<string, string[]>
                {
                    { _client.DstNodeId, ["b1"] }
                }
            };
            await _client.SendAsync(topology);
            var response = await _client.ReadOutputAsync<TopologyOk>();
            Assert.NotNull(response);

            var broadcast = new Broadcast(1);
            await _client.SendAsync(broadcast);
            var response2 = await _client.ReadOutputAsync<BroadcastOk>();
            Assert.NotNull(response2);

            var broadcastRequest1 = await _client.ReadOutputAsync<Broadcast>();
            Assert.NotNull(broadcastRequest1);
            Assert.Equal("b1", broadcastRequest1.Dest);
            // Don't reply to trigger a retry.

            var broadcastRequest2 = await _client.ReadOutputAsync<Broadcast>();
            Assert.NotNull(broadcastRequest2);
            Assert.Equal("b1", broadcastRequest2.Dest);
            Assert.Equal(broadcastRequest1.Body.MsgId + 1, broadcastRequest2.Body.MsgId);
            await _client.SendAsync(GetBroadcastResponse(broadcastRequest2.Body), "b1", _client.DstNodeId);

            var read = new Read();
            await _client.SendAsync(read);
            var readResponse = await _client.ReadOutputAsync<ReadOk>();
            Assert.NotNull(readResponse);
            Assert.Equal([1], readResponse.Body.ReadMessages);
        }

        private static BroadcastOk GetBroadcastResponse(Broadcast request) => new()
        {
            InReplyTo = request.MsgId
        };
    }
}