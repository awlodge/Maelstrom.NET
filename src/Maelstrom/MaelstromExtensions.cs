using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;

namespace Maelstrom;

public static class MaelstromExtensions
{
    public static async Task ReplyAsync(this IMaelstromNode node, Message originalMessage, MessageBody body, CancellationToken cancellationToken)
    {
        if (originalMessage.Body.MsgId == null)
        {
            throw new Exception("For reply, original message must have a MsgId");
        }
        body.InReplyTo = (int)originalMessage.Body.MsgId;
        await node.SendAsync(originalMessage.Src, body, cancellationToken);
    }

    public static async Task ErrorAsync(this IMaelstromNode node, Message originalMessage, ErrorCodes errorCode, string errorMessage, CancellationToken cancellationToken = default)
    {
        var body = new ErrorBody(errorCode, errorMessage);
        await node.ReplyAsync(originalMessage, body, cancellationToken);
    }
}
