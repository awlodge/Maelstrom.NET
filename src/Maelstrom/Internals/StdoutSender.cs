namespace Maelstrom.Internals;

internal class StdoutSender : ISender
{
    private readonly TextWriter _writer;
    public StdoutSender()
    {
        var outputStream = Console.OpenStandardOutput();
        _writer = TextWriter.Synchronized(new StreamWriter(outputStream)
        {
            AutoFlush = true
        });
        Console.SetOut(_writer);
    }
    public async Task SendAsync(string message, CancellationToken cancellationToken) => await _writer.WriteLineAsync(message.AsMemory(), cancellationToken);

    public void Dispose() => _writer.Dispose();
}
