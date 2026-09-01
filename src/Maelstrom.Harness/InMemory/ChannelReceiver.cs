using System.Threading.Channels;

namespace Maelstrom.Harness.InMemory;

internal class ChannelReceiver(Channel<string> input) : IReceiver
{
    private readonly ChannelReader<string> _reader = input.Reader;

    public void Dispose()
    {
    }

    public async Task<string?> RecvAsync(CancellationToken cancellationToken)
    {
        if (await _reader.WaitToReadAsync(cancellationToken))
        {
            _reader.TryRead(out var message);
            return message;
        }

        return null;
    }
}
