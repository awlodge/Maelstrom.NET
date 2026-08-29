using Maelstrom.Internals;
using Microsoft.Extensions.Logging;

namespace Maelstrom;

public static class KvStoreClientExtensions
{
    private const int _defaultMaxAttempts = 10;
    private const int _defaultDelay = 10;

    public static async Task<U> ReadOrDefaultAsync<T, U>(this IKvStoreClient client, T key, U defaultVal, CancellationToken cancellationToken = default)
    {
        try
        {
            return await client.ReadAsync<T, U>(key, cancellationToken);
        }
        catch (KvStoreKeyNotFoundException)
        {
            client.Logger?.LogDebug("Key {key} not found, returning default {default}", key, defaultVal);
            return defaultVal;
        }
    }

    public static async Task<U> SafeUpdateAsync<T, U>(this IKvStoreClient client, T key, Func<U, U> translation, U defaultVal, int maxAttempts = _defaultMaxAttempts, int delayMs = _defaultDelay, CancellationToken cancellationToken = default)
    {
        int attempts = 1;
        while (attempts <= maxAttempts)
        {
            U latestValue = await client.ReadOrDefaultAsync(key, defaultVal, cancellationToken);
            var newValue = translation(latestValue);
            client.Logger?.LogDebug("Update {key} from {old} to {new}, attempt {attempts}", key, latestValue, newValue, attempts);
            try
            {
                await client.CasAsync(key, latestValue, newValue, createIfNotExists: true, cancellationToken: cancellationToken);
            }
            catch (KvStoreCasPreconditionFailed)
            {
                client.Logger?.LogWarning("CAS failed, waiting and retrying");
                await Task.Delay(delayMs + new Random().Next(-2, 2), cancellationToken);
                attempts++;
                continue;
            }

            client.Logger?.LogDebug("Update {key} succeeded", key);
            return newValue;
        }

        client.Logger?.LogError("Update {key} failed after {attempts} attempts", key, maxAttempts);
        throw new KvStoreException($"Update {key} failed after {maxAttempts} attempts");
    }
}
