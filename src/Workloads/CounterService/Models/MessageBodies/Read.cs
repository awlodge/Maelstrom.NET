using Maelstrom.Models;

namespace CounterService.Models.MessageBodies;

internal class Read : MessageBody
{
    public const string ReadType = "read";
}
