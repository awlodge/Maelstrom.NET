using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace KafkaService.Models.MessageBodies;

internal class SendOk : MessageBody
{
    public const string SendOkType = "send_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public SendOk(int offset) : base()
    {
        Type = SendOkType;
        Offset = offset;
    }

    [JsonPropertyName("offset")]
    public required int Offset { get; set; }
}
