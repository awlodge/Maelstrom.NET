using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

[MessageType("poll_ok")]
internal class PollOk : MessageBody
{
    [SetsRequiredMembers]
    public PollOk(Dictionary<string, List<List<int>>> messages) : base()
    {
        Messages = messages;
    }

    [JsonPropertyName("msgs")]
    public required Dictionary<string, List<List<int>>> Messages { get; set; }
}
