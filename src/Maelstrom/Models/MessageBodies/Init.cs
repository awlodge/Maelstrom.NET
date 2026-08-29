using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies;

[MessageType("init")]
public class Init : MessageBody
{
    [SetsRequiredMembers]
    public Init(string nodeId, string[] nodeIds) : base()
    {
        NodeId = nodeId;
        NodeIds = nodeIds;
    }

    [JsonPropertyName("node_id")]
    public required string NodeId { get; set; }

    [JsonPropertyName("node_ids")]
    public required string[] NodeIds { get; set; }
}
