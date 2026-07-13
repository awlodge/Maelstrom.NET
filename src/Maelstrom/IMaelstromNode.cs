using Maelstrom.Models;

namespace Maelstrom;

public interface IMaelstromNode
{
    string NodeId { get; }
    string[] NodeIds { get; }
    IKvStoreClient SeqKvStoreClient { get; }
    IKvStoreClient LinKvStoreClient { get; }
    Task ErrorAsync(Message originalMessage, ErrorCodes errorCode, string errorMessage, CancellationToken cancellationToken = default);
    Task ReplyAsync(Message originalMessage, MessageBody body, CancellationToken cancellationToken = default);
    Task<Message> RpcAsync<T>(string destination, T body, TimeSpan? timeout = null, CancellationToken cancellationToken = default) where T : MessageBody;
}