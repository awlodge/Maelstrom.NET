using Maelstrom.Models;

namespace Maelstrom;

public interface IMaelstromClient
{
    string NodeId { get; }
    Task SendAsync<T>(string destination, T body, CancellationToken cancellationToken = default) where T : MessageBody;
    Task<Message> RpcAsync<T>(string destination, T body, TimeSpan? timeout = null, CancellationToken cancellationToken = default) where T : MessageBody;
}