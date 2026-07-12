using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies.KvStore;

internal class CasOk : MessageBody
{
    public const string CasOkType = "cas_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public CasOk() : base()
    {
        Type = CasOkType;
    }
}