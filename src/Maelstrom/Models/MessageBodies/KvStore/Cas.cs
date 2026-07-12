using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies.KvStore;

internal class Cas<T, U> : MessageBody
{
    public const string CasType = "cas";

    [JsonConstructor]
    [SetsRequiredMembers]
    public Cas(T key, U from, U to, bool createIfNotExists = false) : base()
    {
        Type = CasType;
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
