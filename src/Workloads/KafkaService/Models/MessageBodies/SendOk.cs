using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

[MessageType("send_ok")]
internal class SendOk : MessageBody
{
    [SetsRequiredMembers]
    public SendOk(int offset)
    {
        Offset = offset;
    }

    [JsonPropertyName("offset")]
    public required int Offset { get; set; }
}
