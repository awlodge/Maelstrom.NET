using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

[MessageType("list_committed_offsets_ok")]
internal class ListCommittedOffsetsOk : MessageBody
{
    [SetsRequiredMembers]
    public ListCommittedOffsetsOk(Dictionary<string, int> offsets)
    {
        Offsets = offsets;
    }

    [JsonPropertyName("offsets")]
    public required Dictionary<string, int> Offsets { get; set; }
}
