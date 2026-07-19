using Maelstrom.Models.MessageBodies.KvStore;
using System.Collections.Concurrent;

namespace Maelstrom.TestSupport;

internal class KvStore(IMaelstromTestClient testClient) : IKvStore
{
    private readonly ConcurrentDictionary<string, object> _kvStore = [];

    public async Task<bool> ExpectReadAsync<T>(string serviceName, T expectedKey, TimeSpan timeout = default) where T : IComparable<T>
    {
        var read = await testClient.ReadOutputAsync<Read<T>>(timeout);
        if (read is null || read.Body.Key is null)
        {
            throw new InvalidCastException("Failed to deserialize read");
        }
        if (read.Dest != serviceName)
        {
            throw new ArgumentException($"Expected read for service {serviceName}, received {read.Dest}", nameof(serviceName));
        }
        if (!EqualityComparer<T>.Default.Equals(expectedKey, read.Body.Key))
        {
            throw new ArgumentException($"Expected read for key {expectedKey}, received {read.Body.Key}", nameof(expectedKey));
        }

        var key = read.Body.Key.ToString() ?? throw new ArgumentNullException($"Cannot deserialize key {read.Body.Key}", nameof(read.Body.Key));


        if (!_kvStore.TryGetValue(key, out var value))
        {
            await testClient.ErrorAsync(read, Models.ErrorCodes.KeyDoesNotExist, $"Key {key} not found");
            return false;
        }

        await testClient.ReplyAsync(read, new ReadOk<object>(value));
        return true;
    }

    public async Task<bool> ExpecCasAsync<T, U>(string serviceName, T expectedKey, U expectedFrom, U expectedTo, bool expectedCreateIfNotExists, TimeSpan timeout = default)
        where T : IComparable<T>
        where U : IComparable<U>
    {
        var cas = await testClient.ReadOutputAsync<Cas<T, U>>(timeout);
        if (cas is null || cas.Body.Key is null)
        {
            throw new InvalidCastException("Failed to deserialize CAS");
        }
        if (cas.Dest != serviceName)
        {
            throw new ArgumentException($"Expected CAS for service {serviceName}, received {cas.Dest}", nameof(serviceName));
        }
        if (!EqualityComparer<T>.Default.Equals(expectedKey, cas.Body.Key))
        {
            throw new ArgumentException($"Expected CAS for key {expectedKey}, received {cas.Body.Key}", nameof(expectedKey));
        }

        var key = cas.Body.Key.ToString() ?? throw new ArgumentNullException($"Cannot deserialize key {cas.Body.Key}", nameof(cas.Body.Key));

        if (!EqualityComparer<U>.Default.Equals(expectedFrom, cas.Body.From))
        {
            throw new ArgumentException($"Expected CAS from value {expectedFrom}, received {cas.Body.From}", nameof(expectedFrom));
        }

        if (!EqualityComparer<U>.Default.Equals(expectedTo, cas.Body.To))
        {
            throw new ArgumentException($"Expected CAS to value {expectedTo}, received {cas.Body.To}", nameof(expectedTo));
        }

        if (cas.Body.CreateIfNotExists != expectedCreateIfNotExists)
        {
            throw new ArgumentException($"Expected createIfNotExiste={expectedCreateIfNotExists}, received {cas.Body.CreateIfNotExists}", nameof(expectedCreateIfNotExists));
        }

        if ((!cas.Body.CreateIfNotExists) && !_kvStore.ContainsKey(key))
        {
            await testClient.ErrorAsync(cas, Models.ErrorCodes.KeyDoesNotExist, $"Key {key} does not exist");
            return false;
        }
        if (cas.Body.CreateIfNotExists && !_kvStore.ContainsKey(key))
        {
            if (_kvStore.TryAdd(key, cas.Body.To))
            {
                await testClient.ReplyAsync(cas, new CasOk());
                return true;
            }
        }

        if (!_kvStore.TryUpdate(key, cas.Body.To, cas.Body.From))
        {
            await testClient.ErrorAsync(cas, Models.ErrorCodes.PreconditionFailed, $"Precondition failed: {key} != {cas.Body.From}");
            return false;
        }

        await testClient.ReplyAsync(cas, new CasOk());
        return true;
    }
}
