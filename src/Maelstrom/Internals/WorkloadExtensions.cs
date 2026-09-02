namespace Maelstrom.Internals;

internal static class WorkloadExtensions
{
    internal static IEnumerable<(MaelstromHandlerAttribute, Delegate)> GetHandlers(this Workload workload)
        => MaelstromHandlerAttribute.GetHandlers(workload);
}