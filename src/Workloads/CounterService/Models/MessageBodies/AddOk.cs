using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CounterService.Models.MessageBodies;

internal class AddOk : MessageBody
{
    public const string AddOkType = "add_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public AddOk() : base()
    {
        Type = AddOkType;
    }
}