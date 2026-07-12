using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Maelstrom.Models;

public class Message<T> : Message where T : MessageBody
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [SetsRequiredMembers]
    public Message(string src, string dest, T body) : base(src, dest, null)
    {
        Body = body;
    }

    [SetsRequiredMembers]
    public Message(Message originalMessage) : base(originalMessage.Src, originalMessage.Dest, originalMessage.RawBody)
    {
        Body = RawBody.Deserialize<T>() ?? throw new Exception($"Failed to deserialize message body as {typeof(T)}/");
    }

    [JsonIgnore]
    public override T Body { get; }

    public string Serialize()
    {
        // A bit of a hack until I can figure out a better way to do this.
        RawBody = JsonSerializer.Deserialize<JsonObject>(Body.Serialize() ?? throw new Exception("Failed to serialize body"));
        return JsonSerializer.Serialize(this, _jsonSerializerOptions);
    }
}

[method: JsonConstructor]
public abstract class Message(string src, string dest, JsonObject? rawBody)
{
    [JsonPropertyName("src")]
    [JsonRequired]
    public string Src { get; } = src;

    [JsonPropertyName("dest")]
    [JsonRequired]
    public string Dest { get; } = dest;

    [JsonPropertyName("body")]
    [JsonRequired]
    public JsonObject? RawBody { get; set; } = rawBody;

    [JsonIgnore]
    public abstract MessageBody Body { get; }

    public Message<T> DeserializeAs<T>() where T : MessageBody
    {
        return new Message<T>(this);
    }
}
