using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;

namespace BroadcastService.Models.MessageBodies;

internal class Read : MessageBody
{
    public const string ReadType = "read";

    [SetsRequiredMembers]
    public Read() : base()
    {
        Type = ReadType;
    }
}
