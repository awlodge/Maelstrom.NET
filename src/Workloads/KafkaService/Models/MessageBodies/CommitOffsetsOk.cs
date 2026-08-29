using Maelstrom.Models;

namespace KafkaService.Models.MessageBodies;

[MessageType("commit_offsets_ok")]
internal class CommitOffsetsOk : MessageBody
{
}
