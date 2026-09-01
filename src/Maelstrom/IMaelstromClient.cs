using Maelstrom.Models;

namespace Maelstrom;

public interface IMaelstromClient
{
    string NodeId { get; }
    Task SendAsync<T>(string destination, T body, CancellationToken cancellationToken = default) where T : MessageBody;
    Task<RpcResult<TRecv>> RpcAsync<TSend, TRecv>(string destination, TSend body, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        where TSend : MessageBody
        where TRecv : MessageBody;
    void AddMessageHandler(string messageType, MaelstromHandlerAttribute.MaelstromHandler handler);
}