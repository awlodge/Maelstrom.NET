using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BroadcastService.Models.MessageBodies;

internal class ReadOk : MessageBody
{
    public const string ReadOkType = "read_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public ReadOk(int[] readMessages) : base()
    {
        Type = ReadOkType;
        ReadMessages = readMessages;
    }

    [JsonPropertyName("messages")]
    public required int[] ReadMessages { get; set; }
}
