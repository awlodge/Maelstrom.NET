﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace MaelstromNode.Models.MessageBodies;

internal class InitOk : MessageBody
{
    public const string InitOkType = "init_ok";

    [JsonConstructor]
    [SetsRequiredMembers]
    public InitOk() : base()
    {
        Type = InitOkType;
    }
}
