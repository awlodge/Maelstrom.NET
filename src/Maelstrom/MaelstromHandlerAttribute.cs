namespace Maelstrom;

[AttributeUsage(AttributeTargets.Method)]
public class MaelstromHandlerAttribute(string messageType) : Attribute
{
    public string MessageType { get; } = messageType;
}
