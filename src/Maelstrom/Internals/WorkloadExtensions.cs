using System.Reflection;
using Maelstrom.Internals;

namespace Maelstrom;

internal static class WorkloadExtensions
{
    internal static Dictionary<string, MaelstromNode.MaelstromHandler> GetHandlers(this Workload workload) => workload.GetType()
            .GetMethods()
            .Where(m => m.GetCustomAttributes().OfType<MaelstromHandlerAttribute>().Any())
            .ToDictionary(m => m.GetCustomAttribute<MaelstromHandlerAttribute>()!.MessageType, m => (m.CreateDelegate(typeof(MaelstromNode.MaelstromHandler), workload) as MaelstromNode.MaelstromHandler)!);
}