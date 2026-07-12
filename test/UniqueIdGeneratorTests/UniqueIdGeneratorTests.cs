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
            var generate = new Generate
            {
                Type = Generate.GenerateType
            };
            await client.SendAsync(generate);
            var response = await client.ReadOutputAsync<GenerateOk>();
            Assert.NotNull(response);

            await client.SendAsync(generate);
            var response2 = await client.ReadOutputAsync<GenerateOk>();
            Assert.NotNull(response2);

            Assert.NotEqual(response.Body.Id, response2.Body.Id);
        }
    }
}