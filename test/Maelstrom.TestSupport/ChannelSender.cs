
using System.Threading.Channels;

namespace Maelstrom.TestSupport;

internal class ChannelSender : ISender
{
    private readonly ChannelWriter<string> _writer;

    public ChannelSender(Channel<string> output)
    {
        _writer = output.Writer;
    }

    public void Dispose()
    {
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        await _writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
