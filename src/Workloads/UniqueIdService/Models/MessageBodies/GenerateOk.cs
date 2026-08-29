using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace UniqueIdService.Models.MessageBodies;

[MessageType("generate_ok")]
internal class GenerateOk : MessageBody
{
    [SetsRequiredMembers]
    public GenerateOk(int id) : base()
    {
        Id = id;
    }

    [JsonPropertyName("id")]
    public required int Id { get; set; }
}
