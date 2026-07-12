using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies.KvStore;

internal class Write<T, U> : MessageBody
{
    public const string WriteType = "write";

    [JsonConstructor]
    [SetsRequiredMembers]
    public Write(T key, U value) : base()
    {
        Type = WriteType;
        Key = key;
        Value = value;
    }

    [JsonPropertyName("key")]
    public required T Key { get; set; }

    [JsonPropertyName("value")]
    public required U Value { get; set; }
}
