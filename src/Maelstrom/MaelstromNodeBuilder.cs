using Maelstrom.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Maelstrom;

public static class MaelstromNodeBuilder
{
    public static IServiceCollection AddMaelstromNodeWorkload<TWorkload, TRec, TSend>(this IServiceCollection services)
        where TWorkload : Workload
        where TRec : class, IReceiver
        where TSend : class, ISender
        => services
            .SetupMaelstromNodeDependencies<TRec, TSend>()
            .AddSingleton<TWorkload>()
            .AddSingleton<Workload>(sp => sp.GetRequiredService<TWorkload>());

    public static IServiceCollection AddMaelstromNodeWorkload<TWorkload>(this IServiceCollection services)
        where TWorkload : Workload
        => services.AddMaelstromNodeWorkload<TWorkload, StdinReceiver, StdoutSender>();

    public static IServiceCollection SetupMaelstromNodeDependencies<TRec, TSend>(this IServiceCollection services)
        where TRec : class, IReceiver
        where TSend : class, ISender
    {
        services.TryAddSingleton<IReceiver, TRec>();
        services.TryAddSingleton<ISender, TSend>();
        services.TryAddSingleton<IKvStoreClientFactory, KvStoreClientFactory>();
        services.TryAddSingleton<MaelstromNode>();
        services.TryAddSingleton<IMaelstromNode>(sp => sp.GetRequiredService<MaelstromNode>());
        services.AddHostedService<MaelstromNodeRunner>();
        return services;
    }

    public static async Task RunMaelstromNodeAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        var node = host.Services.GetRequiredService<MaelstromNode>();
        foreach (var workload in host.Services.GetServices<Workload>())
        {
            node.AddMessageHandlers(workload.GetHandlers());
        }
        await host.RunAsync(cancellationToken);
    }
}