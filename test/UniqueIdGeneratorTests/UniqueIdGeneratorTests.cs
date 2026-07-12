using Maelstrom.TestSupport;
using UniqueIdService;
using UniqueIdService.Models.MessageBodies;

namespace UniqueIdGeneratorTests
{
    public class UniqueIdGeneratorTests
    {
        [Fact]
        public async Task TestUniqueIdGenerator()
        {
            await using var client = new MaelstromTestClient<UniqueIdGenerator>();
            await client.StartAsync();
            var echo = new Generate
            {
                Type = Generate.GenerateType
            };
            await client.SendAsync(echo);
            var response = await client.ReadOutputAsync<GenerateOk>();
            Assert.NotNull(response);
            Assert.InRange(response.Body.Id, 0, int.MaxValue);
        }
    }
}