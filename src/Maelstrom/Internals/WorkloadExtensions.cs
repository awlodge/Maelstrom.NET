using System.Reflection;

namespace Maelstrom.Internals;

internal static class WorkloadExtensions
{
    internal static Dictionary<string, MaelstromNode.MaelstromHandler> GetHandlers(this Workload workload) => workload.GetType()
            .GetMethods()
            .Where(m => m.GetCustomAttributes().OfType<MaelstromHandlerAttribute>().Any())
            .ToDictionary(m => m.GetCustomAttribute<MaelstromHandlerAttribute>()!.GetMessageType(), m => (m.CreateDelegate(typeof(MaelstromNode.MaelstromHandler), workload) as MaelstromNode.MaelstromHandler)!);
}