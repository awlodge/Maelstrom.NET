namespace Maelstrom;

public interface IReceiver : IDisposable
{
    Task<string?> RecvAsync(CancellationToken cancellationToken);
}
