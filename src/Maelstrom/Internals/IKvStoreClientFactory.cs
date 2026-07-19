namespace Maelstrom.Internals;

internal interface IKvStoreClientFactory
{
    KvStoreClient Create(string serviceName, IMaelstromNode node);
}