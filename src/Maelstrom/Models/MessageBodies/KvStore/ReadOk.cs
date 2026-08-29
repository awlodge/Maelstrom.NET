using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies.KvStore;

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