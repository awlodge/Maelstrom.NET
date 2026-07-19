using Maelstrom;
using Maelstrom.Internals;
using Maelstrom.Models;
using TransactionRwRegisterService.Models;
using TransactionRwRegisterService.Models.MessageBodies;

namespace TransactionRwRegisterService;

internal class TransactionRwRegister(ILogger<TransactionRwRegister> logger, IMaelstromNode node) : Workload(node)
{
    private const string _transactionIdKey = "transactionId";
    private readonly ILogger<TransactionRwRegister> logger = logger;
    private readonly SemaphoreSlim _getTxnIdLock = new(1);

    [MaelstromHandler(Models.MessageBodies.Transaction.TxnType)]
    public async Task HandleTransaction(Message message, CancellationToken cancellationToken)
    {
        var transaction = message.DeserializeAs<Models.MessageBodies.Transaction>().Body;
        var completedTransactions = await ExecuteTransactions(transaction.Operations, cancellationToken);
        await node.ReplyAsync(message, new TransactionOk(completedTransactions));
    }

    private async Task<List<Operation>> ExecuteTransactions(List<Operation> operations, CancellationToken cancellationToken)
    {
        List<Operation> completedTransactions = [];
        var transactionId = await StartTransaction(cancellationToken);
        Dictionary<int, int> localStore = [];
        try
        {
            foreach (var operation in operations)
            {
                var completedOperation = operation.OperationType switch
                {
                    OperationType.Read => await ExecuteRead(operation, transactionId),
                    OperationType.Write => ExecuteWrite(operation),
                    _ => throw new NotImplementedException(),
                };
                completedTransactions.Add(completedOperation);
            }

            await CommitTransaction(transactionId, localStore, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete transaction: {txnId}", transactionId);
            throw;
        }
        finally
        {
            CompleteTransaction(transactionId);
        }

        return completedTransactions;

        async Task<Operation> ExecuteRead(Operation operation, int transactionId)
        {
            if (localStore.TryGetValue(operation.Key, out int val))
            {
                operation.Val = val;
            }
            else
            {
                operation.Val = await ExecuteReadRemote(operation.Key, transactionId, cancellationToken);
                if (operation.Val != null)
                {
                    localStore[operation.Key] = (int)operation.Val;
                }
            }

            logger.LogDebug("READ:  {k} = {v}", operation.Key, operation.Val);
            return operation;
        }

        Operation ExecuteWrite(Operation operation)
        {
            logger.LogDebug("WRITE: {k} = {v}", operation.Key, operation.Val);
            if (operation.Val == null)
            {
                throw new Exception($"Cannot write null value to key {operation.Key}");
            }

            localStore[operation.Key] = (int)operation.Val;
            return operation;
        }
    }

    private async Task<int> StartTransaction(CancellationToken cancellationToken)
    {
        await _getTxnIdLock.WaitAsync(cancellationToken);
        try
        {
            var transactionId = await IncrementTransactionId(cancellationToken);
            logger.LogInformation("Start transaction: {txnId}", transactionId);
            return transactionId;
        }
        finally
        {
            _getTxnIdLock.Release();
        }
    }

    private async Task<int> IncrementTransactionId(CancellationToken cancellationToken)
    {
        return await node.LinKvStoreClient.SafeUpdateAsync(_transactionIdKey, v => v + 1, 0, cancellationToken: cancellationToken);
    }

    private async Task CommitTransaction(int transactionId, Dictionary<int, int> localStore, CancellationToken cancellationToken)
    {
        logger.LogInformation("Commit transaction: {txnId}", transactionId);
        await Task.WhenAll(localStore.Select(kv => CommitKey(kv.Key, kv.Value, transactionId, cancellationToken)));
        await CommitTransaction(transactionId, cancellationToken);
    }

    private void CompleteTransaction(int transactionId)
    {
        logger.LogInformation("Complete transaction: {txnId}", transactionId);
    }

    private async Task<int?> ExecuteReadRemote(int key, int transactionId, CancellationToken cancellationToken)
    {
        var testTransactionId = transactionId - 1;
        while (testTransactionId > 0)
        {
            if (!await IsTransactionCommitted(testTransactionId, cancellationToken))
            {
                testTransactionId--;
                continue;
            }

            try
            {
                return await ReadRemoteKeyWithTransaction(key, testTransactionId, cancellationToken);
            }
            catch (KvStoreKeyNotFoundException)
            {
                testTransactionId--;
            }

        }

        logger.LogDebug("Key {key} not found in transaction lookback", key);
        return null;
    }

    private async Task CommitKey(int key, int val, int transactionId, CancellationToken cancellationToken)
    {
        await WriteRemoteKeyWithTransaction(key, val, transactionId, cancellationToken);
    }

    private async Task<int> ReadRemoteKeyWithTransaction(int key, int transactionId, CancellationToken cancellationToken) =>
        await node.LinKvStoreClient.ReadAsync<string, int>(GetRemoteKey(key, transactionId), cancellationToken);

    private async Task WriteRemoteKeyWithTransaction(int key, int val, int transactionId, CancellationToken cancellationToken) =>
        await node.LinKvStoreClient.WriteAsync(GetRemoteKey(key, transactionId), val, cancellationToken);

    private async Task CommitTransaction(int transactionId, CancellationToken cancellationToken) =>
        await Node.LinKvStoreClient.WriteAsync(GetCommittedTransactionKey(transactionId), true, cancellationToken);

    private async Task<bool> IsTransactionCommitted(int transactionId, CancellationToken cancellationToken) =>
        await Node.LinKvStoreClient.ReadOrDefaultAsync(GetCommittedTransactionKey(transactionId), false, cancellationToken);

    private static string GetRemoteKey(int key, int transactionId) => $"data/{key}/{transactionId}";

    private static string GetCommittedTransactionKey(int transactionId) => $"committed/{transactionId}";
}