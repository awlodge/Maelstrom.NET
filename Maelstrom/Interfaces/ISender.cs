namespace Maelstrom.Interfaces;

public interface ISender : IDisposable
{
    Task SendAsync(string message, CancellationToken cancellationToken);
}
