using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies.KvStore;

internal class Read<T> : MessageBody
{
    public const string ReadType = "read";

    [JsonConstructor]
    [SetsRequiredMembers]
    public Read(T key) : base()
    {
        Type = ReadType;
        Key = key;
    }

    [JsonPropertyName("key")]
    public required T Key { get; set; }
}
