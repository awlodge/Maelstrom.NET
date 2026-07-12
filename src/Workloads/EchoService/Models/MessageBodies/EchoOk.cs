using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace EchoService.Models.MessageBodies;

internal class EchoOk : MessageBody
{
    public const string EchoOkType = "echo_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public EchoOk(string echoMessage) : base()
    {
        Type = EchoOkType;
        EchoMessage = echoMessage;
    }

    [JsonPropertyName("echo")]
    public required string EchoMessage { get; set; }
}
