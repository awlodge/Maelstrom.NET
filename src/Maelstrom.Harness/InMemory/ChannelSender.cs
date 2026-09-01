using System.Threading.Channels;

namespace Maelstrom.Harness.InMemory;

internal class ChannelSender(Channel<string> output) : ISender
{
    private readonly ChannelWriter<string> _writer = output.Writer;

    public void Dispose()
    {
        _writer.TryComplete();
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        await _writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
