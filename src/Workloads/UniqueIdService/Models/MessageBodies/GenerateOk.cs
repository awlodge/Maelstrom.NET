using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace UniqueIdService.Models.MessageBodies;

internal class GenerateOk : MessageBody
{
    public const string GenerateOkType = "generate_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public GenerateOk(int id) : base()
    {
        Type = GenerateOkType;
        Id = id;
    }

    [JsonPropertyName("id")]
    public required int Id { get; set; }
}
