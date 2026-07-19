using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;

namespace CounterService.Models.MessageBodies;

internal class Read : MessageBody
{
    public const string ReadType = "read";

    [SetsRequiredMembers]
    public Read()
    {
        Type = ReadType;
    }
}
