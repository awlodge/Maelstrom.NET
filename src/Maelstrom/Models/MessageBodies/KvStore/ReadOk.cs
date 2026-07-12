using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies.KvStore;

internal class ReadOk<T> : MessageBody
{
    public const string ReadOkType = "read_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public ReadOk(T value) : base()
    {
        Type = ReadOkType;
        Value = value;
    }

    [JsonPropertyName("value")]
    public required T Value { get; set; }
}