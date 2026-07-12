using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

internal class PollOk : MessageBody
{
    public const string PollOkType = "poll_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public PollOk(Dictionary<string, List<List<int>>> messages) : base()
    {
        Type = PollOkType;
        Messages = messages;
    }

    [JsonPropertyName("msgs")]
    public required Dictionary<string, List<List<int>>> Messages { get; set; }
}
