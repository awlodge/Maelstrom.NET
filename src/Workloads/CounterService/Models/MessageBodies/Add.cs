using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CounterService.Models.MessageBodies;

internal class Add : MessageBody
{
    public const string AddType = "add";

    [JsonConstructor]
    [SetsRequiredMembers]
    public Add(int delta) : base()
    {
        Type = AddType;
        Delta = delta;
    }

    [JsonPropertyName("delta")]
    public required int Delta { get; set; }
}
