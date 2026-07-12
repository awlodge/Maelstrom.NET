using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies;

public class ErrorBody : MessageBody
{
    public const string ErrorBodyType = "error";

    [JsonConstructor]
    [SetsRequiredMembers]
    public ErrorBody(ErrorCodes errorCode, string errorText) : base()
    {
        Type = ErrorBodyType;
        ErrorCode = errorCode;
        ErrorText = errorText;
    }

    [JsonPropertyName("code")]
    public ErrorCodes ErrorCode { get; set; }

    [JsonPropertyName("text")]
    public string? ErrorText { get; set; }
}
