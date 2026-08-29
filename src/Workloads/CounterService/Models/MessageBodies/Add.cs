using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CounterService.Models.MessageBodies;

[MessageType("add")]
internal class Add : MessageBody
{
    [SetsRequiredMembers]
    public Add(int delta)
    {
        Delta = delta;
    }

    [JsonPropertyName("delta")]
    public required int Delta { get; set; }
}
