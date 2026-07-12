using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

internal class CommitOffsets : MessageBody
{
    public const string CommitOffsetsType = "commit_offsets";

    [JsonPropertyName("offsets")]
    public required Dictionary<string, int> Offsets { get; set; }
}
