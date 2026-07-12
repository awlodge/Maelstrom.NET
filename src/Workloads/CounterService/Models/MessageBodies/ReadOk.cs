using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CounterService.Models.MessageBodies;

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