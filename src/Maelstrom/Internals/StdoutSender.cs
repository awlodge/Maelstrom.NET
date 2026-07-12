namespace Maelstrom.Internals;

internal class StdoutSender : ISender
{
    private readonly StreamWriter _stream;
    public StdoutSender()
    {
        var outputStream = Console.OpenStandardOutput();
        _stream = new StreamWriter(outputStream)
        {
            AutoFlush = true
        };
        Console.SetOut(_stream);
    }
    public async Task SendAsync(string message, CancellationToken cancellationToken) => await _stream.WriteLineAsync(message.AsMemory(), cancellationToken);

    public void Dispose() => _stream.Dispose();
}
