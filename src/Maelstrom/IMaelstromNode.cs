using Maelstrom.Models;

namespace Maelstrom;

public interface IMaelstromNode
{
    string NodeId { get; }
    string[] NodeIds { get; }
    Task SendAsync<T>(string destination, T body, CancellationToken cancellationToken = default) where T : MessageBody;
    Task<Message> RpcAsync<T>(string destination, T body, TimeSpan? timeout = null, CancellationToken cancellationToken = default) where T : MessageBody;
}