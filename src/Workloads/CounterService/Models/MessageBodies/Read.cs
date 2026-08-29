using Maelstrom.Models;

namespace CounterService.Models.MessageBodies;

[MessageType("read")]
internal class Read : MessageBody
{
}
