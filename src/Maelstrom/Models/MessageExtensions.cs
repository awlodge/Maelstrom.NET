using Maelstrom.Models.MessageBodies;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Maelstrom.Models;

public static class MessageExtensions
{
    public static bool TryDeserializeAs<T>(this Message message, [NotNullWhen(true)] out Message<T>? deserializedMessage) where T : MessageBody
    {
        deserializedMessage = null;
        if (message.Body.Type == MessageTypeAttribute.GetMessageType<T>())
        {
            deserializedMessage = new Message<T>(message);
            return true;
        }
        return false;
    }

    public static Message<T> DeserializeAs<T>(this Message message) where T : MessageBody
    {
        if (!message.TryDeserializeAs<T>(out var deserializedMessage))
        {
            throw new JsonException($"Incorrect message type '{message.Body.Type}' to deserialize to {typeof(T)}");
        }
        return deserializedMessage;
    }

    public static bool IsError(this Message message, [NotNullWhen(true)] out ErrorBody? error)
    {
        error = null;
        if (message.TryDeserializeAs<ErrorBody>(out var errorMessage))
        {
            error = errorMessage.Body;
            return true;
        }

        return false;
    }
}
