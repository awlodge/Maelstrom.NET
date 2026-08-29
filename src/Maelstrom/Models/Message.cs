using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Maelstrom.Models;

public class Message<T> : Message where T : MessageBodyBase
{
    [SetsRequiredMembers]
    [JsonConstructor]
    public Message(string src, string dest, JsonObject rawBody)
    {
        Src = src;
        Dest = dest;
        RawBody = rawBody;
        Body = RawBody.Deserialize<T>(_jsonSerializerOptions) ?? throw new JsonException($"Failed to deserialize message body as {typeof(T)}/");
    }

    [SetsRequiredMembers]
    public Message(string src, string dest, T body)
    {
        Src = src;
        Dest = dest;
        Body = body;
    }

    [SetsRequiredMembers]
    public Message(Message originalMessage)
    {
        Src = originalMessage.Src;
        Dest = originalMessage.Dest;
        RawBody = originalMessage.RawBody;
        Body = RawBody.Deserialize<T>(_jsonSerializerOptions) ?? throw new JsonException($"Failed to deserialize message body as {typeof(T)}/");
    }

    [JsonIgnore]
    public override T Body { get; }

    public string Serialize()
    {
        // A bit of a hack until I can figure out a better way to do this.
        RawBody = JsonSerializer.Deserialize<JsonObject>(
            Body.Serialize(_jsonSerializerOptions) ?? throw new JsonException("Failed to serialize body"),
            _jsonSerializerOptions);
        return JsonSerializer.Serialize(this, _jsonSerializerOptions);
    }
}

public abstract class Message
{
    internal static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [JsonPropertyName("src")]
    public required string Src { get; set; }

    [JsonPropertyName("dest")]
    public required string Dest { get; set; }

    [JsonPropertyName("body")]
    [JsonRequired]
    public JsonObject? RawBody { get; set; }

    [JsonIgnore]
    public abstract MessageBodyBase Body { get; }

    public static Message? Deserialize(string input)
    {
        return JsonSerializer.Deserialize<Message<MessageBodyBase>>(input, _jsonSerializerOptions);
    }
}
