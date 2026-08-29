using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Maelstrom.Models.MessageBodies;

[MessageType("error")]
public class ErrorBody : MessageBody
{
    [SetsRequiredMembers]
    public ErrorBody(ErrorCodes errorCode, string errorText)
    {
        ErrorCode = errorCode;
        ErrorText = errorText;
    }

    [JsonPropertyName("code")]
    public ErrorCodes ErrorCode { get; set; }

    [JsonPropertyName("text")]
    public string? ErrorText { get; set; }
}
