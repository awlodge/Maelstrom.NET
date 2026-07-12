using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

internal class ListCommittedOffsets : MessageBody
{
    public const string ListCommittedOffsetsType = "list_committed_offsets";

    [JsonPropertyName("keys")]
    public required List<string> Keys { get; set; }
}
