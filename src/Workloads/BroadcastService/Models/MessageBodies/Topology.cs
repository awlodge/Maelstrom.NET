using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace BroadcastService.Models.MessageBodies;

internal class Topology : MessageBody
{
    public const string TopologyType = "topology";

    [JsonPropertyName("topology")]
    public required Dictionary<string, string[]> TopologyData { get; set; }
}
