using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maelstrom.Models;

public abstract class MessageBody : MessageBodyBase
{
    public override string Type
    {
        get => MessageTypeAttribute.GetMessageType(this.GetType());
        init { }
    }
}
public class MessageBodyBase
{
    [JsonPropertyName("type")]
    [JsonRequired]
    public virtual string Type { get; init; }

    [JsonPropertyName("msg_id")]
    public int? MsgId { get; set; }

    [JsonPropertyName("in_reply_to")]
    public int? InReplyTo { get; set; }

    internal string? Serialize(JsonSerializerOptions options) => JsonSerializer.Serialize<object>(this, options);
}
