using Maelstrom.Models;
using System.Text.Json.Serialization;

namespace TransactionRwRegisterService.Models.MessageBodies;

internal class Transaction : MessageBody
{
    public const string TxnType = "txn";

    [JsonPropertyName("txn")]
    [JsonConverter(typeof(OperationListConverter))]
    public required List<Operation> Operations { get; set; }
}
