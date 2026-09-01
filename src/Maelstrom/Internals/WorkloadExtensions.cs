namespace Maelstrom.Internals;

internal static class WorkloadExtensions
{
    internal static Dictionary<string, MaelstromHandlerAttribute.MaelstromHandler> GetHandlers(this Workload workload)
        => MaelstromHandlerAttribute.GetHandlers(workload);
}