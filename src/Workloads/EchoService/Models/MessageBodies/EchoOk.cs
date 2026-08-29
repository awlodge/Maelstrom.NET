using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EchoService.Models.MessageBodies;

[MessageType("echo_ok")]
internal class EchoOk : MessageBody
{
    [SetsRequiredMembers]
    public EchoOk(string echoMessage)
    {
        EchoMessage = echoMessage;
    }

    [JsonPropertyName("echo")]
    public required string EchoMessage { get; set; }
}
