using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

[MessageType("send")]
internal class Send : MessageBody
{
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("msg")]
    public required int Message { get; set; }
}
