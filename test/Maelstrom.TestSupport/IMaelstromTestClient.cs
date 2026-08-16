using Maelstrom.Models;

namespace Maelstrom.TestSupport;

public interface IMaelstromTestClient : IMaelstromClient
{
    Task<Message<T>> ReadOutputAsync<T>(TimeSpan timeout = default) where T : MessageBody;
    Task SendAsync<T>(T body) where T : MessageBody;
    Task SendAsync<T>(T body, string src, string dst) where T : MessageBody;
}