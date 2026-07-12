
namespace Maelstrom.Interfaces;

public interface IKvStoreClient
{
    Task CasAsync<T, U>(T key, U from, U to, bool createIfNotExists = false);
    Task<U> ReadAsync<T, U>(T key);
    Task<U> ReadOrDefaultAsync<T, U>(T key, U defaultVal);
    Task<U> SafeUpdateAsync<T, U>(T key, Func<U, U> translation, U defaultVal, int maxAttempts = 10, int delayMs = 10);
    Task WriteAsync<T, U>(T key, U value);
}