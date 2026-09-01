using Maelstrom.Harness.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Maelstrom.Harness;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryHarness(this IServiceCollection services)
    {
        services.TryAddSingleton<MaelstromBus>();
        services.TryAddSingleton<InMemoryHarness>();
        services.TryAddSingleton<MaelstromHarness>(sp => sp.GetRequiredService<InMemoryHarness>());
        return services;
    }

    public static InMemoryHarness GetInMemoryHarness(this IHost host) => host.Services.GetRequiredService<InMemoryHarness>();
}
