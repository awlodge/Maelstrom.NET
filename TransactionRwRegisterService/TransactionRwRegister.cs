using Maelstrom;
using Maelstrom.Interfaces;
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
    public async Task HandleTransaction(Message message)
    {
        var transaction = message.DeserializeAs<Models.MessageBodies.Transaction>().Body;
        var completedTransactions = await ExecuteTransactions(transaction.Operations);
        await node.ReplyAsync(message, new TransactionOk(completedTransactions));
    }

    private async Task<List<Operation>> ExecuteTransactions(List<Operation> operations)
    {
        List<Operation> completedTransactions = [];
        var transactionId = await StartTransaction();
        Dictionary<int, int> localStore = [];
        try
        {
            foreach (var operation in operations)
            {
                var completedOperation = operation.OperationType switch
                {
                    OperationType.Read => await ExecuteRead(operation, transactionId, localStore),
                    OperationType.Write => ExecuteWrite(operation, localStore),
                    _ => throw new NotImplementedException(),
                };
                completedTransactions.Add(completedOperation);
            }

            await CommitTransaction(transactionId, localStore);
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
    }

    private async Task<int> StartTransaction()
    {
        await _getTxnIdLock.WaitAsync();
        try
        {
            var transactionId = await IncrementTransactionId();
            logger.LogInformation("Start transaction: {txnId}", transactionId);
            return transactionId;
        }
        finally
        {
            _getTxnIdLock.Release();
        }
    }

    private async Task<int> IncrementTransactionId()
    {
        return await node.LinKvStoreClient.SafeUpdateAsync(_transactionIdKey, v => v + 1, 0);
    }

    private async Task CommitTransaction(int transactionId, Dictionary<int, int> localStore)
    {
        // For every KV pair in _localStore, write data/key/_transactionId = val to the KvStore.
        // For every key in _localStore, try to set key/lastCommitted to _transactionId.
        // If this fails and key/lastCommitted is lower than _transactionId retry, otherwise abort.
        logger.LogInformation("Commit transaction: {txnId}", transactionId);
        await Task.WhenAll(localStore.Select(kv => CommitKey(kv.Key, kv.Value, transactionId)));
    }

    private void CompleteTransaction(int transactionId)
    {
        logger.LogInformation("Complete transaction: {txnId}", transactionId);
    }

    private Operation ExecuteWrite(Operation operation, Dictionary<int, int> localStore)
    {
        logger.LogDebug("WRITE: {k} = {v}", operation.Key, operation.Val);
        if (operation.Val == null)
        {
            throw new Exception($"Cannot write null value to key {operation.Key}");
        }

        localStore[operation.Key] = (int)operation.Val;
        return operation;
    }

    private async Task<Operation> ExecuteRead(Operation operation, int transactionId, Dictionary<int, int> localStore)
    {
        if (localStore.TryGetValue(operation.Key, out int val))
        {
            operation.Val = val;
        }
        else
        {
            operation.Val = await ExecuteReadRemote(operation.Key, transactionId);
            if (operation.Val != null)
            {
                localStore[operation.Key] = (int)operation.Val;
            }
        }

        logger.LogDebug("READ:  {k} = {v}", operation.Key, operation.Val);
        return operation;
    }

    private async Task<int?> ExecuteReadRemote(int key, int transactionId)
    {
        // Look for value of lastCommitted/key in KvStore.
        // If it is not there, return null.
        // If it is lower than the current transaction id, return data/key/x.
        // Otherwise lookback through data/key/n for n decreasing from the current transaction id.
        int lastCommittedTransaction;
        try
        {
            lastCommittedTransaction = await GetLastCommittedTransaction(key);
        }
        catch (KvStoreKeyNotFoundException)
        {
            return null;
        }

        if (lastCommittedTransaction < transactionId)
        {
            return await ReadRemoteKeyWithTransaction(key, lastCommittedTransaction);
        }

        int testTransactionId = transactionId - 1;
        while (testTransactionId > 0)
        {
            try
            {
                return await ReadRemoteKeyWithTransaction(key, testTransactionId);
            }
            catch (KvStoreKeyNotFoundException)
            {
                testTransactionId--;
            }
        }

        logger.LogDebug("Key {key} not found in transaction lookback", key);
        return null;
    }

    private async Task CommitKey(int key, int val, int transactionId)
    {
        await WriteWremoteKeyWithTransaction(key, val, transactionId);
        await TrySetLastCommittedTransaction(key, transactionId);
    }

    private async Task TrySetLastCommittedTransaction(int key, int transactionId)
    {
        int attempts = 1;
        int maxAttempts = 10;

        while (attempts <= maxAttempts)
        {
            int latestTransactionId;
            try
            {
                latestTransactionId = await GetLastCommittedTransaction(key);
            }
            catch (KvStoreKeyNotFoundException)
            {
                latestTransactionId = 0;
            }

            if (latestTransactionId > transactionId)
            {
                logger.LogWarning("Transaction {txnId} for key {key} already overtaken by transaction {latest}", transactionId, key, latestTransactionId);
                return;
            }

            logger.LogDebug("Update lastCommited/{key} from {old} to {new}, attempt {attempts}", key, latestTransactionId, transactionId, attempts);
            try
            {
                await node.LinKvStoreClient.CasAsync(GetLastCommittedKey(key), latestTransactionId, transactionId, createIfNotExists: true);
            }
            catch (KvStoreCasPreconditionFailed)
            {
                logger.LogWarning("CAS failed, retrying");
                attempts++;
                continue;
            }

            logger.LogDebug("Update {key} succeeded", key);
            return;
        }

        logger.LogError("Update last committed  {key} failed after {attempts} attempts", key, maxAttempts);
        throw new KvStoreException($"Update last committed {key} failed after {maxAttempts} attempts");
    }

    private async Task<int> ReadRemoteKeyWithTransaction(int key, int transactionId) => await node.LinKvStoreClient.ReadAsync<string, int>(GetRemoteKey(key, transactionId));

    private async Task<int> GetLastCommittedTransaction(int key) => await node.LinKvStoreClient.ReadAsync<string, int>(GetLastCommittedKey(key));

    private async Task WriteWremoteKeyWithTransaction(int key, int val, int transactionId) => await node.LinKvStoreClient.WriteAsync(GetRemoteKey(key, transactionId), val);

    private static string GetRemoteKey(int key, int transactionId) => $"data/{key}/{transactionId}";

    private static string GetLastCommittedKey(int key) => $"lastCommitted/{key}";
}
