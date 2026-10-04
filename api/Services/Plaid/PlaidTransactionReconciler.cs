using Cardui.Api.Data;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Services.Interfaces;
using Going.Plaid.Entity;
using Microsoft.EntityFrameworkCore;
using Account = Cardui.Api.Models.Account;
using FinancialRecordProvenance = Cardui.Api.Models.FinancialRecordProvenance;
using FinancialRecordSource = Cardui.Api.Models.FinancialRecordSource;
using StoredTransaction = Cardui.Api.Models.Transaction;

namespace Cardui.Api.Services.Plaid;

internal enum TransactionUpsertResult
{
    Skipped,
    Added,
    Modified
}

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
        var candidateIds = incoming
            .SelectMany(x => new[] { x.TransactionId, x.PendingTransactionId })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var accountIds = accountsByPlaidId.Values
            .Select(x => x.Id)
            .ToList();

        var existingByPlaidId = candidateIds.Count == 0
            ? new Dictionary<string, StoredTransaction>(StringComparer.Ordinal)
            : await _dbContext.Transactions
                .Where(x =>
                    accountIds.Contains(x.AccountId)
                    && x.PlaidTransactionId != null
                    && candidateIds.Contains(x.PlaidTransactionId))
                .ToDictionaryAsync(
                    x => x.PlaidTransactionId!,
                    StringComparer.Ordinal,
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

        var promotedPendingIds = incoming
            .Select(x => x.PendingTransactionId)
            .Where(x =>
                !string.IsNullOrWhiteSpace(x)
                && initiallyStoredIds.Contains(x)
                && !existingByPlaidId.ContainsKey(x))
            .ToHashSet(StringComparer.Ordinal);
        var removedIds = removed
            .Select(x => x.TransactionId)
            .Where(x =>
                !string.IsNullOrWhiteSpace(x)
                && !promotedPendingIds.Contains(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var removedCount = 0;
        if (removedIds.Count > 0)
        {
            var transactionsToRemove = await _dbContext.Transactions
                .Where(x =>
                    accountIds.Contains(x.AccountId)
                    && x.PlaidTransactionId != null
                    && removedIds.Contains(x.PlaidTransactionId))
                .ToListAsync(cancellationToken);

            _dbContext.Transactions.RemoveRange(transactionsToRemove);
            removedCount = transactionsToRemove.Count;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TransactionSyncPageResultDto
        {
            Added = addedCount,
            Modified = modifiedCount,
            Removed = removedCount
        };
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
        if (string.IsNullOrWhiteSpace(plaidTransaction.TransactionId))
        {
            throw new InvalidOperationException("Plaid transaction is missing transaction id.");
        }

        if (plaidTransaction.Date is null)
        {
            throw new InvalidOperationException(
                $"Plaid transaction {plaidTransaction.TransactionId} is missing date.");
        }

        if (string.IsNullOrWhiteSpace(plaidTransaction.AccountId)
            || !accountsByPlaidId.TryGetValue(plaidTransaction.AccountId, out var account))
        {
            _logger.LogWarning(
                "Skipping Plaid transaction {TransactionId} because account {PlaidAccountId} was not found for Plaid item {PlaidItemId}",
                plaidTransaction.TransactionId,
                plaidTransaction.AccountId,
                plaidItemId);

            return TransactionUpsertResult.Skipped;
        }

        existingByPlaidId.TryGetValue(
            plaidTransaction.TransactionId,
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
            existingTransaction.PlaidTransactionId = plaidTransaction.TransactionId;
            existingByPlaidId[plaidTransaction.TransactionId] = existingTransaction;
        }

        var name = plaidTransaction.OriginalDescription
                   ?? plaidTransaction.MerchantName
                   ?? "Unknown transaction";

        if (existingTransaction is null)
        {
            var categoryId = await _transactionCategorizationService
                .GetCategoryIdForPlaidTransactionAsync(
                    plaidTransaction,
                    cancellationToken);

            var transaction = new StoredTransaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                PlaidTransactionId = plaidTransaction.TransactionId,
                Source = FinancialRecordSource.Plaid,
                Provenance = FinancialRecordProvenance.PlaidSync,
                Date = plaidTransaction.Date.Value,
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
            existingByPlaidId[plaidTransaction.TransactionId] = transaction;
            return TransactionUpsertResult.Added;
        }

        existingTransaction.AccountId = account.Id;
        if (!existingTransaction.IsDateUserEdited)
        {
            existingTransaction.Date = plaidTransaction.Date.Value;
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

        return TransactionUpsertResult.Modified;
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
        }
    }
}
