namespace Maelstrom;

public interface IKvStoreClient
{
    Task CasAsync<T, U>(T key, U from, U to, bool createIfNotExists = false, CancellationToken cancellationToken = default);
    Task<U> ReadAsync<T, U>(T key, CancellationToken cancellationToken = default);
    Task<U> ReadOrDefaultAsync<T, U>(T key, U defaultVal, CancellationToken cancellationToken = default);
    Task<U> SafeUpdateAsync<T, U>(T key, Func<U, U> translation, U defaultVal, int maxAttempts = 10, int delayMs = 10, CancellationToken cancellationToken = default);
    Task WriteAsync<T, U>(T key, U value, CancellationToken cancellationToken = default);
}