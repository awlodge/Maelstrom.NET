using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace EchoService.Models.MessageBodies;

[MessageType("echo")]
internal class Echo : MessageBody
{
    [JsonPropertyName("echo")]
    public required string EchoMessage { get; set; }
}
