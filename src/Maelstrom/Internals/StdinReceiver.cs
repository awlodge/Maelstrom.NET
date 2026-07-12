namespace Maelstrom.Internals;

internal class StdinReceiver : IReceiver
{
    private readonly StreamReader _stream;

    public StdinReceiver()
    {
        var inputStream = Console.OpenStandardInput();
        _stream = new StreamReader(inputStream);
    }

    public async Task<string?> RecvAsync(CancellationToken cancellationToken) =>
        await _stream.ReadLineAsync(cancellationToken);

    public void Dispose() => _stream.Dispose();
}
