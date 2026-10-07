using Cardui.Api.Data;
using Cardui.Api.Domain.Plaid;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Services.Interfaces;
using Going.Plaid.Entity;
using Microsoft.EntityFrameworkCore;
using Account = Cardui.Api.Models.Account;
using FinancialRecordProvenance = Cardui.Api.Models.FinancialRecordProvenance;
using FinancialRecordSource = Cardui.Api.Models.FinancialRecordSource;
using StoredTransaction = Cardui.Api.Models.Transaction;

namespace Cardui.Api.Services.Plaid;

public class PlaidTransactionReconciler : IPlaidTransactionReconciler
{
    private readonly CarduiDBContext _dbContext;
    private readonly ITransactionCategorizationService _transactionCategorizationService;
    private readonly ILogger<PlaidTransactionReconciler> _logger;

    public PlaidTransactionReconciler(
        CarduiDBContext dbContext,
        ITransactionCategorizationService transactionCategorizationService,
        ILogger<PlaidTransactionReconciler> logger)
    {
        _dbContext = dbContext;
        _transactionCategorizationService = transactionCategorizationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TransactionSyncPageResultDto> ReconcileAsync(
        Guid plaidItemId,
        IReadOnlyDictionary<string, Account> accountsByPlaidId,
        IReadOnlyCollection<Transaction> added,
        IReadOnlyCollection<Transaction> modified,
        IReadOnlyCollection<RemovedTransaction> removed,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var incoming = added.Concat(modified).ToList();
        var existingByPlaidId = await LoadExistingTransactionsAsync(
            incoming,
            accountsByPlaidId,
            cancellationToken);
        var initiallyStoredIds = existingByPlaidId.Keys.ToHashSet(StringComparer.Ordinal);

        var addedCount = 0;
        var modifiedCount = 0;

        foreach (var plaidTransaction in added)
        {
            var result = await UpsertAsync(
                plaidTransaction,
                plaidItemId,
                accountsByPlaidId,
                existingByPlaidId,
                now,
                cancellationToken);
            Count(result, ref addedCount, ref modifiedCount);
        }

        foreach (var plaidTransaction in modified)
        {
            var result = await UpsertAsync(
                plaidTransaction,
                plaidItemId,
                accountsByPlaidId,
                existingByPlaidId,
                now,
                cancellationToken);
            Count(result, ref addedCount, ref modifiedCount);
        }

        var removedCount = await RemoveTransactionsAsync(
            incoming,
            removed,
            accountsByPlaidId,
            initiallyStoredIds,
            existingByPlaidId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TransactionSyncPageResultDto
        {
            Added = addedCount,
            Modified = modifiedCount,
            Removed = removedCount
        };
    }

    #region Private Methods

    /// <summary>
    /// Loads stored rows whose Plaid id matches an incoming transaction or its pending id.
    /// </summary>
    private async Task<Dictionary<string, StoredTransaction>> LoadExistingTransactionsAsync(
        IReadOnlyList<Transaction> incoming,
        IReadOnlyDictionary<string, Account> accountsByPlaidId,
        CancellationToken cancellationToken)
    {
        var candidateIds = incoming
            .SelectMany(transaction => new[] { transaction.TransactionId, transaction.PendingTransactionId })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (candidateIds.Count == 0)
        {
            return new Dictionary<string, StoredTransaction>(StringComparer.Ordinal);
        }

        var accountIds = accountsByPlaidId.Values
            .Select(account => account.Id)
            .ToList();

        return await _dbContext.Transactions
            .Where(transaction =>
                accountIds.Contains(transaction.AccountId)
                && transaction.PlaidTransactionId != null
                && candidateIds.Contains(transaction.PlaidTransactionId))
            .ToDictionaryAsync(
                transaction => transaction.PlaidTransactionId!,
                StringComparer.Ordinal,
                cancellationToken);
    }

    /// <summary>
    /// Inserts a transaction or updates the stored row, including a pending
    /// row that Plaid has now posted. Skips a transaction whose account is unknown.
    /// </summary>
    private async Task<TransactionUpsertResult> UpsertAsync(
        Transaction plaidTransaction,
        Guid plaidItemId,
        IReadOnlyDictionary<string, Account> accountsByPlaidId,
        IDictionary<string, StoredTransaction> existingByPlaidId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var transactionId = RequirePlaidIdentity(plaidTransaction);

        var account = ResolveAccount(plaidTransaction, plaidItemId, accountsByPlaidId);
        if (account is null)
        {
            return TransactionUpsertResult.Skipped;
        }

        var existingTransaction = FindStoredTransaction(
            plaidTransaction,
            transactionId,
            existingByPlaidId);
        var name = DisplayName(plaidTransaction);

        if (existingTransaction is null)
        {
            await AddTransactionAsync(
                plaidTransaction,
                transactionId,
                account,
                name,
                existingByPlaidId,
                now,
                cancellationToken);
            return TransactionUpsertResult.Added;
        }

        await ApplyPlaidUpdateAsync(
            existingTransaction,
            plaidTransaction,
            account,
            name,
            now,
            cancellationToken);
        return TransactionUpsertResult.Modified;
    }

    /// <summary>
    /// Rejects a Plaid transaction that has no id or no date, and returns the id.
    /// </summary>
    private static string RequirePlaidIdentity(Transaction plaidTransaction)
    {
        var transactionId = plaidTransaction.TransactionId;
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            throw new InvalidOperationException("Plaid transaction is missing transaction id.");
        }

        if (plaidTransaction.Date is null)
        {
            throw new InvalidOperationException(
                $"Plaid transaction {transactionId} is missing date.");
        }

        return transactionId;
    }

    /// <summary>
    /// Resolves the household account for a Plaid transaction.
    /// An unknown account is logged and skipped.
    /// </summary>
    private Account? ResolveAccount(
        Transaction plaidTransaction,
        Guid plaidItemId,
        IReadOnlyDictionary<string, Account> accountsByPlaidId)
    {
        if (!string.IsNullOrWhiteSpace(plaidTransaction.AccountId)
            && accountsByPlaidId.TryGetValue(plaidTransaction.AccountId, out var account))
        {
            return account;
        }

        _logger.LogWarning(
            "Skipping Plaid transaction {TransactionId} because account {PlaidAccountId} was not found for Plaid item {PlaidItemId}",
            plaidTransaction.TransactionId,
            plaidTransaction.AccountId,
            plaidItemId);

        return null;
    }

    /// <summary>
    /// Finds the stored row by Plaid id, or by the pending id when Plaid has posted it.
    /// A promoted pending row keeps the new posted id.
    /// </summary>
    private static StoredTransaction? FindStoredTransaction(
        Transaction plaidTransaction,
        string transactionId,
        IDictionary<string, StoredTransaction> existingByPlaidId)
    {
        existingByPlaidId.TryGetValue(
            transactionId,
            out var existingTransaction);

        if (existingTransaction is null
            && !string.IsNullOrWhiteSpace(plaidTransaction.PendingTransactionId)
            && existingByPlaidId.TryGetValue(
                plaidTransaction.PendingTransactionId,
                out var pendingTransaction)
            && pendingTransaction.Pending)
        {
            existingTransaction = pendingTransaction;
            existingByPlaidId.Remove(plaidTransaction.PendingTransactionId);
            existingTransaction.PlaidTransactionId = transactionId;
            existingByPlaidId[transactionId] = existingTransaction;
        }

        return existingTransaction;
    }

    /// <summary>
    /// Uses the original description, then the merchant name, then a fallback label.
    /// </summary>
    private static string DisplayName(Transaction plaidTransaction)
    {
        return plaidTransaction.OriginalDescription
            ?? plaidTransaction.MerchantName
            ?? "Unknown transaction";
    }

    /// <summary>
    /// Inserts a categorized transaction and remembers it for later rows in this page.
    /// </summary>
    private async Task AddTransactionAsync(
        Transaction plaidTransaction,
        string transactionId,
        Account account,
        string name,
        IDictionary<string, StoredTransaction> existingByPlaidId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var categoryId = await _transactionCategorizationService
            .GetCategoryIdForPlaidTransactionAsync(
                plaidTransaction,
                cancellationToken);

        var transaction = new StoredTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            PlaidTransactionId = transactionId,
            Source = FinancialRecordSource.Plaid,
            Provenance = FinancialRecordProvenance.PlaidSync,
            Date = plaidTransaction.Date!.Value,
            AuthorizedDate = plaidTransaction.AuthorizedDate,
            Name = name,
            MerchantName = plaidTransaction.MerchantName,
            Amount = Convert.ToDecimal(plaidTransaction.Amount),
            IsoCurrencyCode = plaidTransaction.IsoCurrencyCode,
            Pending = plaidTransaction.Pending ?? false,
            CategoryId = categoryId,
            Notes = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Transactions.Add(transaction);
        existingByPlaidId[transactionId] = transaction;
    }

    /// <summary>
    /// Copies bank fields onto a stored row. A user-edited date or category is left unchanged.
    /// </summary>
    private async Task ApplyPlaidUpdateAsync(
        StoredTransaction existingTransaction,
        Transaction plaidTransaction,
        Account account,
        string name,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        existingTransaction.AccountId = account.Id;
        if (!existingTransaction.IsDateUserEdited)
        {
            existingTransaction.Date = plaidTransaction.Date!.Value;
        }

        existingTransaction.AuthorizedDate = plaidTransaction.AuthorizedDate;
        existingTransaction.Name = name;
        existingTransaction.MerchantName = plaidTransaction.MerchantName;
        existingTransaction.Amount = Convert.ToDecimal(plaidTransaction.Amount);
        existingTransaction.IsoCurrencyCode = plaidTransaction.IsoCurrencyCode;
        existingTransaction.Pending = plaidTransaction.Pending ?? false;
        existingTransaction.UpdatedAt = now;

        if (existingTransaction.CategoryId is null
            && !existingTransaction.IsCategoryUserEdited)
        {
            existingTransaction.CategoryId = await _transactionCategorizationService
                .GetCategoryIdForPlaidTransactionAsync(
                    plaidTransaction,
                    cancellationToken);
        }
    }

    /// <summary>
    /// Deletes stored rows Plaid removed, except a pending id that was just posted.
    /// </summary>
    private async Task<int> RemoveTransactionsAsync(
        IReadOnlyList<Transaction> incoming,
        IReadOnlyCollection<RemovedTransaction> removed,
        IReadOnlyDictionary<string, Account> accountsByPlaidId,
        IReadOnlySet<string> initiallyStoredIds,
        IReadOnlyDictionary<string, StoredTransaction> existingByPlaidId,
        CancellationToken cancellationToken)
    {
        var promotedPendingIds = incoming
            .Select(transaction => transaction.PendingTransactionId)
            .Where(id =>
                !string.IsNullOrWhiteSpace(id)
                && initiallyStoredIds.Contains(id)
                && !existingByPlaidId.ContainsKey(id))
            .ToHashSet(StringComparer.Ordinal);
        var removedIds = removed
            .Select(transaction => transaction.TransactionId)
            .Where(id =>
                !string.IsNullOrWhiteSpace(id)
                && !promotedPendingIds.Contains(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (removedIds.Count == 0)
        {
            return 0;
        }

        var accountIds = accountsByPlaidId.Values
            .Select(account => account.Id)
            .ToList();
        var transactionsToRemove = await _dbContext.Transactions
            .Where(transaction =>
                accountIds.Contains(transaction.AccountId)
                && transaction.PlaidTransactionId != null
                && removedIds.Contains(transaction.PlaidTransactionId))
            .ToListAsync(cancellationToken);

        _dbContext.Transactions.RemoveRange(transactionsToRemove);
        return transactionsToRemove.Count;
    }

    /// <summary>
    /// Adds one to the added or modified count. A skipped transaction is not counted.
    /// </summary>
    private static void Count(
        TransactionUpsertResult result,
        ref int addedCount,
        ref int modifiedCount)
    {
        switch (result)
        {
            case TransactionUpsertResult.Added:
                addedCount++;
                break;
            case TransactionUpsertResult.Modified:
                modifiedCount++;
                break;
            case TransactionUpsertResult.Skipped:
                break;
        }
    }

    #endregion
}
