using Maelstrom.Models;

namespace Maelstrom.TestSupport;

public interface IMaelstromTestClient : IMaelstromClient
{
    Task<Message<T>> ReadOutputAsync<T>(TimeSpan timeout = default) where T : MessageBody;
    Task SendAsync<T>(T body, CancellationToken cancellationToken = default) where T : MessageBody;
    Task SendAsync<T>(T body, string src, string dst, CancellationToken cancellationToken = default) where T : MessageBody;
}