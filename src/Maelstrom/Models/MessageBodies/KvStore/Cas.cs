using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies.KvStore;

[MessageType("cas")]
internal class Cas<T, U> : MessageBody
{
    [SetsRequiredMembers]
    public Cas(T key, U from, U to, bool createIfNotExists = false)
    {
        Key = key;
        From = from;
        To = to;
        CreateIfNotExists = createIfNotExists;
    }

    [JsonPropertyName("key")]
    public required T Key { get; set; }

    [JsonPropertyName("from")]
    public required U From { get; set; }

    [JsonPropertyName("to")]
    public required U To { get; set; }

    [JsonPropertyName("create_if_not_exists")]
    public bool CreateIfNotExists { get; set; }
}
