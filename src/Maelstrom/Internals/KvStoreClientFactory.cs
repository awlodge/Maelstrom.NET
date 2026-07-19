using Microsoft.Extensions.Logging;

namespace Maelstrom.Internals;

internal class KvStoreClientFactory(ILoggerFactory loggerFactory) : IKvStoreClientFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    public KvStoreClient Create(string serviceName, IMaelstromNode node) =>
        new(node, _loggerFactory.CreateLogger<KvStoreClient>(), serviceName);
}
