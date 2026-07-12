using Maelstrom.Interfaces;

namespace Maelstrom;

public class Workload(IMaelstromNode node)
{
    protected IMaelstromNode Node => node;
}
