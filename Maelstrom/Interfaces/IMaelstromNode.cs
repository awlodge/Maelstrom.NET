using Maelstrom.Models;

namespace Maelstrom.Interfaces;

public interface IMaelstromNode
{
    string NodeId { get; }
    string[] NodeIds { get; }
    IKvStoreClient SeqKvStoreClient { get; }
    IKvStoreClient LinKvStoreClient { get; }
    Task ErrorAsync(Message originalMessage, ErrorCodes errorCode, string errorMessage);
    Task ReplyAsync(Message originalMessage, MessageBody body);
    Task<Message> RpcAsync<T>(string destination, T body) where T : MessageBody;
}