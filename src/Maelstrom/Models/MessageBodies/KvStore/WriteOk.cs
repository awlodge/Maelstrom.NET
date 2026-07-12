using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies.KvStore;

internal class WriteOk : MessageBody
{
    public const string WriteOkType = "write_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public WriteOk() : base()
    {
        Type = WriteOkType;
    }
}