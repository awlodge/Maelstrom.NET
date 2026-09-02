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
    public delegate Task MaelstromHandler<T>(Message<T> msg, CancellationToken cancellationToken = default) where T : MessageBody;

    internal abstract Type MessageType { get; }

    internal string GetMessageType() => MessageTypeAttribute.GetMessageType(MessageType);

    internal static IEnumerable<(MaelstromHandlerAttribute, Delegate)> GetHandlers(object o) => o.GetType().GetMethods()
        .Where(m => m.GetCustomAttributes().OfType<MaelstromHandlerAttribute>().Any())
        .Select(m =>
        {
            var attr = m.GetCustomAttribute<MaelstromHandlerAttribute>()!;
            var handlerDelegate = typeof(MaelstromHandler<>)!.MakeGenericType(attr.MessageType)!;
            return (attr, m.CreateDelegate(handlerDelegate, o));
        });
}