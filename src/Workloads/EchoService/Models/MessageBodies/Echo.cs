using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace EchoService.Models.MessageBodies;

internal class Echo : MessageBody
{
    public const string EchoType = "echo";

    [JsonPropertyName("echo")]
    public required string EchoMessage { get; set; }
}
