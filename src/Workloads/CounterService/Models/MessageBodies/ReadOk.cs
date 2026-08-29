using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CounterService.Models.MessageBodies;

[MessageType("read_ok")]
internal class ReadOk<T> : MessageBody
{
    [SetsRequiredMembers]
    public ReadOk(T value)
    {
        Value = value;
    }

    [JsonPropertyName("value")]
    public required T Value { get; set; }
}