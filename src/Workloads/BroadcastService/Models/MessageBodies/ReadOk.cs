using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BroadcastService.Models.MessageBodies;

[MessageType("read_ok")]
internal class ReadOk : MessageBody
{
    [SetsRequiredMembers]
    public ReadOk(int[] readMessages)
    {
        ReadMessages = readMessages;
    }

    [JsonPropertyName("messages")]
    public required int[] ReadMessages { get; set; }
}
