using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

[MessageType("poll")]
internal class Poll : MessageBody
{
    [JsonPropertyName("offsets")]
    public required Dictionary<string, int> Offsets { get; set; }
}
