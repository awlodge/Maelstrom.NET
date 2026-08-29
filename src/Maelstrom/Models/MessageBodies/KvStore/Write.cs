using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies.KvStore;

[MessageType("write")]
internal class Write<T, U> : MessageBody
{
    [SetsRequiredMembers]
    public Write(T key, U value)
    {
        Key = key;
        Value = value;
    }

    [JsonPropertyName("key")]
    public required T Key { get; set; }

    [JsonPropertyName("value")]
    public required U Value { get; set; }
}
