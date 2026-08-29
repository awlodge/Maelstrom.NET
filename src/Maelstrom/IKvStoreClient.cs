using Microsoft.Extensions.Logging;

namespace Maelstrom;

public interface IKvStoreClient
{
    Task CasAsync<T, U>(T key, U from, U to, bool createIfNotExists = false, CancellationToken cancellationToken = default);
    Task<U> ReadAsync<T, U>(T key, CancellationToken cancellationToken = default);
    Task WriteAsync<T, U>(T key, U value, CancellationToken cancellationToken = default);

    ILogger? Logger { get; }
}