using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Maelstrom.Internals;

internal class MaelstromClientFactory : IMaelstromClientFactory
{
    private readonly IServiceProvider _serviceProvider;

    internal MaelstromClientFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public MaelstromClient CreateMaelstromClient(string nodeId)
    {
        return new MaelstromClient(nodeId,
            _serviceProvider.GetRequiredService<ILogger<MaelstromClient>>(),
            _serviceProvider.GetRequiredService<IReceiver>(),
            _serviceProvider.GetRequiredService<ISender>());
    }
}
