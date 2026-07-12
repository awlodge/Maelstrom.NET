using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

internal class Poll : MessageBody
{
    public const string PollType = "poll";

    [JsonPropertyName("offsets")]
    public required Dictionary<string, int> Offsets { get; set; }
}
