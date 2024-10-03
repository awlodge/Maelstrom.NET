﻿using Maelstrom.Interfaces;
using Maelstrom.Models;
using Maelstrom.Models.MessageBodies;
using Maelstrom.Models.MessageBodies.KvStore;
using Microsoft.Extensions.Logging;

namespace Maelstrom;

internal class KvStoreClient(IMaelstromNode node, ILogger<IMaelstromNode> logger, string serviceName) : IKvStoreClient
{
    private const int _defaultMaxAttempts = 10;
    private const int _defaultDelay = 10;

    private readonly string _serviceName = serviceName;
    private readonly ILogger<IMaelstromNode> logger = logger;
    private readonly IMaelstromNode _node = node;

    public async Task<U> ReadAsync<T, U>(T key)
    {
        logger.LogDebug("Reading key {key}", key);
        Read<T> read = new(key);
        var response = await _node.RpcAsync(_serviceName, read);
        if (response.Body.Type == ErrorBody.ErrorBodyType)
        {
            response.DeserializeAs<ErrorBody>();
            var error = (ErrorBody)response.Body;
            logger.LogDebug("Error reading key {key}: {errorCode} {errorText}", key, error.ErrorCode, error.ErrorText);
            if (error.ErrorCode == ErrorCodes.KeyDoesNotExist)
            {
                throw new KvStoreKeyNotFoundException($"Key {key} does not exist");
            }

            throw new KvStoreException($"Error reading key {key}: {error.ErrorText}");
        }

        response.DeserializeAs<ReadOk<U>>();
        var readOk = (ReadOk<U>)response.Body;
        logger.LogDebug("Read key {key}: {value}", key, readOk.Value);
        return readOk.Value;
    }

    public async Task<U> ReadOrDefaultAsync<T, U>(T key, U defaultVal)
    {
        try
        {
            return await ReadAsync<T, U>(key);
        }
        catch (KvStoreKeyNotFoundException)
        {
            logger.LogDebug("Key {key} not found, returning default {default}", key, defaultVal);
            return defaultVal;
        }
    }

    public async Task WriteAsync<T, U>(T key, U value)
    {
        logger.LogDebug("Writing key {key}: {value}", key, value);
        Write<T, U> write = new(key, value);
        var response = await _node.RpcAsync(_serviceName, write);
        switch (response.Body.Type)
        {
            case ErrorBody.ErrorBodyType:
                response.DeserializeAs<ErrorBody>();
                var error = (ErrorBody)response.Body;
                throw new KvStoreException($"Error writing key {key}: {error.ErrorText}");

            case WriteOk.WriteOkType:
                logger.LogDebug("Wrote key {key}: {value}", key, value);
                break;

            default:
                throw new Exception($"Unexpected return type for key write: {response.Body.Type}");
        }
    }

    public async Task CasAsync<T, U>(T key, U from, U to, bool createIfNotExists = false)
    {
        logger.LogDebug("CAS key {key} from {from} to {to}", key, from, to);
        Cas<T, U> cas = new(key, from, to, createIfNotExists);
        var response = await _node.RpcAsync(_serviceName, cas);
        switch (response.Body.Type)
        {
            case ErrorBody.ErrorBodyType:
                response.DeserializeAs<ErrorBody>();
                var error = (ErrorBody)response.Body;
                logger.LogDebug("Error setting key {key}: {errorCode} {errorText}", key, error.ErrorCode, error.ErrorText);

                throw error.ErrorCode switch
                {
                    ErrorCodes.KeyDoesNotExist => new KvStoreKeyNotFoundException($"Key {key} does not exist"),
                    ErrorCodes.PreconditionFailed => new KvStoreCasPreconditionFailed($"CAS precondition failed for key {key}"),
                    _ => new KvStoreException($"Error setting key {key}: {error.ErrorText}"),
                };
            case CasOk.CasOkType:
                logger.LogDebug("CAS key {key} from {from} to {to} succeeded", key, from, to);
                break;

            default:
                throw new Exception($"Unexpected return type for CAS operation: {response.Body.Type}");
        }
    }

    public async Task<U> SafeUpdateAsync<T, U>(T key, Func<U, U> translation, U defaultVal, int maxAttempts = _defaultMaxAttempts, int delayMs = _defaultDelay)
    {
        int attempts = 1;
        while (attempts <= maxAttempts)
        {
            U latestValue = await ReadOrDefaultAsync(key, defaultVal);
            var newValue = translation(latestValue);
            logger.LogDebug("Update {key} from {old} to {new}, attempt {attempts}", key, latestValue, newValue, attempts);
            try
            {
                await CasAsync(key, latestValue, newValue, createIfNotExists: true);
            }
            catch (KvStoreCasPreconditionFailed)
            {
                logger.LogWarning("CAS failed, waiting and retrying");
                await Task.Delay(delayMs + new Random().Next(-2, 2));
                attempts++;
                continue;
            }

            logger.LogDebug("Update {key} succeeded", key);
            return newValue;
        }

        logger.LogError("Update {key} failed after {attempts} attempts", key, maxAttempts);
        throw new KvStoreException($"Update {key} failed after {maxAttempts} attempts");
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