using Maelstrom.Models;

namespace Maelstrom;

[AttributeUsage(AttributeTargets.Method)]
public class MaelstromHandlerAttribute<T>() : MaelstromHandlerAttribute where T : MessageBody
{
    internal override Type MessageType { get; } = typeof(T);
}

public abstract class MaelstromHandlerAttribute : Attribute
{
    internal abstract Type MessageType { get; }

    internal string GetMessageType() => MessageTypeAttribute.GetMessageType(MessageType);
}