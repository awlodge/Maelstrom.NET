using System.Reflection;

namespace Maelstrom.Models;

[AttributeUsage(AttributeTargets.Class)]
public class MessageTypeAttribute(string messageType) : Attribute
{
    internal string MessageType => messageType;

    internal static string GetMessageType(Type messageType)
    {
        if (!messageType.IsSubclassOf(typeof(MessageBody)))
        {
            throw new InvalidOperationException($"Cannot get message type of {messageType} as it does not derive from MessageBody");
        }
        return messageType.GetCustomAttribute<MessageTypeAttribute>()?.MessageType
            ?? throw new InvalidOperationException($"Type {messageType} does not have the MessageType attribute");
    }

    internal static string GetMessageType<T>() where T : MessageBody
        => GetMessageType(typeof(T));
}
