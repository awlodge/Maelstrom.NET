using Maelstrom.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TransactionRwRegisterService.Models.MessageBodies
{
    internal class TransactionOk : MessageBody
    {
        public const string TxnOkType = "txn_ok";

        [JsonConstructor]
        [SetsRequiredMembers]
        public TransactionOk(List<Operation> operations) : base()
        {
            Type = TxnOkType;
            Operations = operations;
        }

        [JsonPropertyName("txn")]
        [JsonConverter(typeof(OperationListConverter))]
        public required List<Operation> Operations { get; set; }
    }
}
