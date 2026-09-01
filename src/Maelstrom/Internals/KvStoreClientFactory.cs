using Microsoft.Extensions.Logging;

namespace Maelstrom.Internals;

internal class KvStoreClientFactory(ILoggerFactory loggerFactory) : IKvStoreClientFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public KvStoreClient Create(string serviceName, IMaelstromClient client) =>
        new(client, _loggerFactory.CreateLogger<KvStoreClient>(), serviceName);
}
