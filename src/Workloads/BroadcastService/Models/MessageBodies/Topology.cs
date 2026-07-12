using Maelstrom.Models;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace BroadcastService.Models.MessageBodies;

internal class Topology : MessageBody
{
    public const string TopologyType = "topology";

    [JsonPropertyName("topology")]
    public required JsonObject TopologyData { get; set; }
}
