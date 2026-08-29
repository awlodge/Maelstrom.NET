using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace BroadcastService.Models.MessageBodies;

[MessageType("topology")]
internal class Topology : MessageBody
{
    [JsonPropertyName("topology")]
    public required Dictionary<string, string[]> TopologyData { get; set; }
}
