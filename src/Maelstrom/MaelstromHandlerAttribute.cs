using Maelstrom.Models;
using System.Reflection;

namespace Maelstrom;

[AttributeUsage(AttributeTargets.Method)]
public class MaelstromHandlerAttribute<T>() : MaelstromHandlerAttribute where T : MessageBody
{
    internal override Type MessageType { get; } = typeof(T);
}

public abstract class MaelstromHandlerAttribute : Attribute
{
    public delegate Task MaelstromHandler(Message msg, CancellationToken cancellationToken = default);

    internal abstract Type MessageType { get; }

    internal string GetMessageType() => MessageTypeAttribute.GetMessageType(MessageType);

    internal static Dictionary<string, MaelstromHandler> GetHandlers(object o) => o.GetType().GetMethods()
        .Where(m => m.GetCustomAttributes().OfType<MaelstromHandlerAttribute>().Any())
        .ToDictionary(m => m.GetCustomAttribute<MaelstromHandlerAttribute>()!.GetMessageType(), m => (m.CreateDelegate(typeof(MaelstromHandler), o) as MaelstromHandler)!);
}