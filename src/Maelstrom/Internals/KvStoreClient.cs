using Maelstrom.Models;
using Maelstrom.Models.MessageBodies.KvStore;
using Microsoft.Extensions.Logging;

namespace Maelstrom.Internals;

internal class KvStoreClient(IMaelstromClient client, ILogger<KvStoreClient> logger, string serviceName) : IKvStoreClient
{
    private readonly string _serviceName = serviceName;
    private readonly ILogger<KvStoreClient> logger = logger;
    private readonly IMaelstromClient _client = client;

    public ILogger Logger => logger;

    public async Task<U> ReadAsync<T, U>(T key, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Reading key {key}", key);
        var result = await _client.RpcAsync<Read<T>, ReadOk<U>>(_serviceName, new(key), cancellationToken: cancellationToken);
        if (result.IsError(out var error))
        {
            logger.LogDebug("Error reading key {key}: {errorCode} {errorText}", key, error.ErrorCode, error.ErrorText);
            if (error.ErrorCode == ErrorCodes.KeyDoesNotExist)
            {
                throw new KvStoreKeyNotFoundException($"Key {key} does not exist");
            }

            throw new KvStoreException($"Error reading key {key}: {error.ErrorText}");
        }

        var readOk = result.Result;
        logger.LogDebug("Read key {key}: {value}", key, readOk.Value);
        return readOk.Value;
    }

    public async Task WriteAsync<T, U>(T key, U value, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Writing key {key}: {value}", key, value);
        Write<T, U> write = new(key, value);
        var result = await _client.RpcAsync<Write<T, U>, WriteOk>(_serviceName, write, cancellationToken: cancellationToken);
        if (result.IsError(out var error))
        {
            throw new KvStoreException($"Error writing key {key}: {error.ErrorText}");
        }

        logger.LogDebug("Wrote key {key}: {value}", key, value);
    }

    public async Task CasAsync<T, U>(T key, U from, U to, bool createIfNotExists = false, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("CAS key {key} from {from} to {to}", key, from, to);
        Cas<T, U> cas = new(key, from, to, createIfNotExists);
        var result = await _client.RpcAsync<Cas<T, U>, CasOk>(_serviceName, cas, cancellationToken: cancellationToken);
        if (result.IsError(out var error))
        {
            logger.LogDebug("Error setting key {key}: {errorCode} {errorText}", key, error.ErrorCode, error.ErrorText);

            throw error.ErrorCode switch
            {
                ErrorCodes.KeyDoesNotExist => new KvStoreKeyNotFoundException($"Key {key} does not exist"),
                ErrorCodes.PreconditionFailed => new KvStoreCasPreconditionFailed($"CAS precondition failed for key {key}"),
                _ => new KvStoreException($"Error setting key {key}: {error.ErrorText}"),
            };
        }

        logger.LogDebug("CAS key {key} from {from} to {to} succeeded", key, from, to);
    }
}

public class KvStoreException(string message) : Exception(message)
{
}

public class KvStoreKeyNotFoundException(string message) : KvStoreException(message)
{
}

public class KvStoreCasPreconditionFailed(string message) : KvStoreException(message)
{
}