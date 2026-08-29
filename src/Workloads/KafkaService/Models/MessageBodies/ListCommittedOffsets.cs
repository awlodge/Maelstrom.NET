using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

[MessageType("list_committed_offsets")]
internal class ListCommittedOffsets : MessageBody
{
    [JsonPropertyName("keys")]
    public required List<string> Keys { get; set; }
}
