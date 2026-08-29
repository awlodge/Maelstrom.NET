using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

[MessageType("commit_offsets")]
internal class CommitOffsets : MessageBody
{
    [JsonPropertyName("offsets")]
    public required Dictionary<string, int> Offsets { get; set; }
}
