using Maelstrom.Harness.InMemory;
using Maelstrom.TestSupport;
using TransactionRwRegisterService;
using TransactionRwRegisterService.Models;
using TransactionRwRegisterService.Models.MessageBodies;

namespace TransactionRwRegisterTests
{
    public class TransactionRwRegisterTests : InMemoryHarnessTests
    {
        [Fact]
        public async Task TestReadWriteReadTransaction()
        {
            var transaction = new Transaction
            {
                Operations = [
                    Operation.Read(1),
                    Operation.Write(1, 2),
                    Operation.Read(1),
                ]
            };
            var txnResult = await RpcAsync<Transaction, TransactionOk>(transaction);
            Assert.True(txnResult.IsSuccess);
            Assert.Equivalent(
                new List<Operation> {
                    Operation.Read(1),
                    Operation.Write(1, 2),
                    Operation.Read(1, 2)
                },
                txnResult.Result.Operations);

            // Follow up to ensure read value persists
            var readResult = await RpcAsync<Transaction, TransactionOk>(new Transaction
            {
                Operations = [Operation.Read(1)]
            });
            Assert.True(readResult.IsSuccess);
            Assert.Equivalent(
                new List<Operation>
                {
                    Operation.Read(1, 2),
                },
                readResult.Result.Operations);
        }

        protected override InMemoryHarness SetupHarness(InMemoryHarness harness) => harness.AddWorkload<TransactionRwRegister>();
    }
}