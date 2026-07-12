namespace Maelstrom;

public interface ISender : IDisposable
{
    Task SendAsync(string message, CancellationToken cancellationToken);
}
