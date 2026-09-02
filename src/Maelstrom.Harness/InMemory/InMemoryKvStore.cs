using Maelstrom.Models;
using Maelstrom.Models.MessageBodies.KvStore;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Maelstrom.Harness.InMemory;

internal class InMemoryKvStore
{
    private readonly IMaelstromClient _client;
    private readonly ConcurrentDictionary<object, object> _store = [];

    public InMemoryKvStore(IMaelstromClient client)
    {
        _client = client;
    }

    [MaelstromHandler<Read<JsonElement>>]
    public async Task HandleRead(Message<Read<JsonElement>> message, CancellationToken cancellationToken = default)
    {
        if (!TryParseElement(message.Body.Key, out var key))
        {
            await _client.ErrorAsync(message, ErrorCodes.MalformedRequest, $"Cannot parse key '{message.Body.Key}'", cancellationToken);
            return;
        }
        if (!_store.TryGetValue(key, out var val))
        {
            await _client.ErrorAsync(message, ErrorCodes.KeyDoesNotExist, $"Key '{key}' not found", cancellationToken);
            return;
        }
        var readOk = new ReadOk<object>(val);
        await _client.ReplyAsync(message, readOk, cancellationToken);
    }

    [MaelstromHandler<Write<JsonElement, JsonElement>>]
    public async Task HandleWrite(Message<Write<JsonElement, JsonElement>> message, CancellationToken cancellationToken = default)
    {
        if (!TryParseElement(message.Body.Key, out var key))
        {
            await _client.ErrorAsync(message, ErrorCodes.MalformedRequest, $"Cannot parse key '{message.Body.Key}'", cancellationToken);
            return;
        }
        if (!TryParseElement(message.Body.Value, out var val))
        {
            await _client.ErrorAsync(message, ErrorCodes.MalformedRequest, $"Cannot parse value '{message.Body.Value}'", cancellationToken);
            return;
        }

        _store[key] = val;
        await _client.ReplyAsync(message, new WriteOk(), cancellationToken);
    }

    [MaelstromHandler<Cas<JsonElement, JsonElement>>]

    public async Task HandleCas(Message<Cas<JsonElement, JsonElement>> message, CancellationToken cancellationToken = default)
    {
        var cas = message.Body;
        if (!TryParseElement(cas.Key, out var key))
        {
            await _client.ErrorAsync(message, ErrorCodes.MalformedRequest, $"Cannot parse key '{cas.Key}'", cancellationToken);
            return;
        }
        if (!TryParseElement(cas.From, out var from))
        {
            await _client.ErrorAsync(message, ErrorCodes.MalformedRequest, $"Cannot parse from value '{cas.From}'", cancellationToken);
            return;
        }
        if (!TryParseElement(cas.To, out var to))
        {
            await _client.ErrorAsync(message, ErrorCodes.MalformedRequest, $"Cannot parse to value '{cas.To}'", cancellationToken);
            return;
        }

        if (cas.CreateIfNotExists && _store.TryAdd(key, to))
        {
            await _client.ReplyAsync(message, new CasOk(), cancellationToken);
            return;
        }

        if (!cas.CreateIfNotExists && !_store.ContainsKey(key))
        {
            await _client.ErrorAsync(message, ErrorCodes.KeyDoesNotExist, $"CAS failed: key '{key}' already exists", cancellationToken);
            return;
        }

        if (!_store.TryUpdate(key, to, from))
        {
            await _client.ErrorAsync(message, ErrorCodes.PreconditionFailed, $"Precondition failed for key '{key}'", cancellationToken);
            return;
        }

        await _client.ReplyAsync(message, new CasOk(), cancellationToken);
    }

    private static bool TryParseElement(JsonElement key, [NotNullWhen(true)] out object? parsed)
    {
        parsed = key.ValueKind switch
        {
            JsonValueKind.String => key.GetString(),
            JsonValueKind.Number => key.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
        return parsed != null;
    }
}
