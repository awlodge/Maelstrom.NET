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
                b.Services.AddHostedService<BroadcastServicePoller>();
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
        public async Task TestReadToTopologyNeighbors()
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

            var readRequest1 = await _client.ReadOutputAsync<Read>(timeout: TimeSpan.FromSeconds(2));
            Assert.NotNull(readRequest1);
            Assert.Equal("b1", readRequest1.Dest);
            await _client.SendAsync(new ReadOk([]) { InReplyTo = readRequest1.Body.MsgId });


            var readRequest2 = await _client.ReadOutputAsync<Read>(timeout: TimeSpan.FromSeconds(2));
            Assert.NotNull(readRequest2);
            Assert.Equal("b2", readRequest2.Dest);
            await _client.SendAsync(new ReadOk([1]) { InReplyTo = readRequest2.Body.MsgId });

            var read = new Read();
            await _client.SendAsync(read);
            var readResponse = await _client.ReadOutputAsync<ReadOk>();
            Assert.NotNull(readResponse);
            Assert.Equal([1], readResponse.Body.ReadMessages);
        }

        private static BroadcastOk GetBroadcastResponse(Broadcast request) => new BroadcastOk()
        {
            InReplyTo = request.MsgId
        };
    }
}