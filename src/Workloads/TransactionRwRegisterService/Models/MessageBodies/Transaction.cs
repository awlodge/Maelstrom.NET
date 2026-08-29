using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace TransactionRwRegisterService.Models.MessageBodies;

[MessageType("txn")]
internal class Transaction : MessageBody
{
    [JsonPropertyName("txn")]
    [JsonConverter(typeof(OperationListConverter))]
    public required List<Operation> Operations { get; set; }
}
