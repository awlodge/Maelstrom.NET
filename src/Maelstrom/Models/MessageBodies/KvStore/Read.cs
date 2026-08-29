using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies.KvStore;

[MessageType("read")]
internal class Read<T> : MessageBody
{
    [SetsRequiredMembers]
    public Read(T key)
    {
        Key = key;
    }

    [JsonPropertyName("key")]
    public required T Key { get; set; }
}
