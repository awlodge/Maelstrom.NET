namespace Maelstrom;

public interface IMaelstromNode : IMaelstromClient
{
    string[] NodeIds { get; }
}