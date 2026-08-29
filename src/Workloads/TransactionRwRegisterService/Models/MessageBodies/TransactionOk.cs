using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TransactionRwRegisterService.Models.MessageBodies;

[MessageType("txn_ok")]
internal class TransactionOk : MessageBody
{
    [SetsRequiredMembers]
    public TransactionOk(List<Operation> operations) : base()
    {
        Operations = operations;
    }

    [JsonPropertyName("txn")]
    [JsonConverter(typeof(OperationListConverter))]
    public required List<Operation> Operations { get; set; }
}
