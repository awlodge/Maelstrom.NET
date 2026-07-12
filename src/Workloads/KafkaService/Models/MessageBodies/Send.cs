using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

internal class Send : MessageBody
{
    public const string SendType = "send";

    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("msg")]
    public required int Message { get; set; }
}
