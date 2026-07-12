using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

internal class ListCommittedOffsetsOk : MessageBody
{
    public const string ListCommittedOffsetsOkType = "list_committed_offsets_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public ListCommittedOffsetsOk(Dictionary<string, int> offsets) : base()
    {
        Type = ListCommittedOffsetsOkType;
        Offsets = offsets;
    }

    [JsonPropertyName("offsets")]
    public required Dictionary<string, int> Offsets { get; set; }
}
