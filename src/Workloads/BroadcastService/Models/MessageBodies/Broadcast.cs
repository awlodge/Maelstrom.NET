using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace BroadcastService.Models.MessageBodies;

[MessageType("broadcast")]
internal class Broadcast : MessageBody
{
    [SetsRequiredMembers]
    public Broadcast(int broadcastMessage)
    {
        BroadcastMessage = broadcastMessage;
    }

    [JsonPropertyName("message")]
    public required int BroadcastMessage { get; set; }
}
