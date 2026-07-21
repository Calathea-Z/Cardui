using Cardui.Api.Data;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Transactions;
using Microsoft.EntityFrameworkCore;
using Account = Cardui.Api.Models.Account;
using Transaction = Cardui.Api.Models.Transaction;

namespace Cardui.Api.Services.Plaid;

internal enum TransactionUpsertResult
{
    Skipped,
    Added,
    Modified
}

public class PlaidTransactionSyncService : IPlaidTransactionSyncService
{
    private readonly CarduiDBContext _dbContext;
    private readonly PlaidClient _plaidClient;
    private readonly IPlaidRequestExecutor _requestExecutor;
    private readonly IPlaidAccessTokenProtector _accessTokenProtector;
    private readonly ITransactionCategorizationService _transactionCategorizationService;
    private readonly ITransferPairingService _transferPairingService;
    private readonly ILogger<PlaidTransactionSyncService> _logger;
    private readonly TimeProvider _timeProvider;

    public PlaidTransactionSyncService(
        CarduiDBContext dbContext,
        PlaidClient plaidClient,
        IPlaidRequestExecutor requestExecutor,
        IPlaidAccessTokenProtector accessTokenProtector,
        ITransactionCategorizationService transactionCategorizationService,
        ITransferPairingService transferPairingService,
        ILogger<PlaidTransactionSyncService> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _plaidClient = plaidClient;
        _requestExecutor = requestExecutor;
        _accessTokenProtector = accessTokenProtector;
        _transactionCategorizationService = transactionCategorizationService;
        _transferPairingService = transferPairingService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<SyncTransactionsResponseDto> SyncTransactionsForPlaidItemAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken = default)
    {
        var addedCount = 0;
        var modifiedCount = 0;
        var removedCount = 0;
        var hasMore = true;
        var cursor = plaidItem.TransactionsCursor;
        var accessToken = _accessTokenProtector.Unprotect(plaidItem.AccessToken);

        var accountsByPlaidId = await _dbContext.Accounts
            .Where(x => x.PlaidItemId == plaidItem.Id)
            .ToDictionaryAsync(x => x.PlaidAccountId, cancellationToken);

        while (hasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = _requestExecutor.WithCredentials(new TransactionsSyncRequest
            {
                Cursor = cursor,
                Count = 100,
                Options = new TransactionsSyncRequestOptions
                {
                    IncludeOriginalDescription = true
                }
            }, accessToken);

            var response = await _requestExecutor.ExecuteAsync(
                () => _plaidClient.TransactionsSyncAsync(request));

            var now = _timeProvider.GetUtcNow();

            var pageTransactionIds = response.Added
                .Concat(response.Modified)
                .Select(x => x.TransactionId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var existingTransactionsByPlaidId = pageTransactionIds.Count == 0
                ? new Dictionary<string, Transaction>()
                : await _dbContext.Transactions
                    .Where(x => pageTransactionIds.Contains(x.PlaidTransactionId))
                    .ToDictionaryAsync(x => x.PlaidTransactionId, cancellationToken);

            foreach (var plaidTransaction in response.Added)
            {
                var result = await UpsertPlaidTransactionAsync(
                    plaidTransaction,
                    plaidItem.Id,
                    accountsByPlaidId,
                    existingTransactionsByPlaidId,
                    now,
                    cancellationToken);

                CountUpsert(result, ref addedCount, ref modifiedCount);
            }

            foreach (var plaidTransaction in response.Modified)
            {
                var result = await UpsertPlaidTransactionAsync(
                    plaidTransaction,
                    plaidItem.Id,
                    accountsByPlaidId,
                    existingTransactionsByPlaidId,
                    now,
                    cancellationToken);

                CountUpsert(result, ref addedCount, ref modifiedCount);
            }

            var removedTransactionIds = response.Removed
                .Select(x => x.TransactionId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (removedTransactionIds.Count > 0)
            {
                var transactionsToRemove = await _dbContext.Transactions
                    .Where(x => removedTransactionIds.Contains(x.PlaidTransactionId))
                    .ToListAsync(cancellationToken);

                foreach (var existingTransaction in transactionsToRemove)
                {
                    _dbContext.Transactions.Remove(existingTransaction);
                    removedCount++;
                }
            }

            cursor = response.NextCursor;
            hasMore = response.HasMore;

            plaidItem.TransactionsCursor = cursor;
            plaidItem.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        plaidItem.LastTransactionsSyncedAt = _timeProvider.GetUtcNow();
        plaidItem.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        await CategorizeUncategorizedForPlaidItemAsync(plaidItem.Id, cancellationToken);

        await _transferPairingService.PairOwnedAccountTransfersAsync(cancellationToken);

        return new SyncTransactionsResponseDto
        {
            Added = addedCount,
            Modified = modifiedCount,
            Removed = removedCount,
            NextCursor = cursor
        };
    }

    private async Task<TransactionUpsertResult> UpsertPlaidTransactionAsync(
        Going.Plaid.Entity.Transaction plaidTransaction,
        Guid plaidItemId,
        IReadOnlyDictionary<string, Account> accountsByPlaidId,
        IDictionary<string, Transaction> existingTransactionsByPlaidId,
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

        existingTransactionsByPlaidId.TryGetValue(
            plaidTransaction.TransactionId,
            out var existingTransaction);

        var name = plaidTransaction.MerchantName
                   ?? plaidTransaction.OriginalDescription
                   ?? "Unknown transaction";

        if (existingTransaction is null)
        {
            var categoryId = await _transactionCategorizationService
                .GetCategoryIdForPlaidTransactionAsync(
                    plaidTransaction,
                    cancellationToken);

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                PlaidTransactionId = plaidTransaction.TransactionId,
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
            existingTransactionsByPlaidId[plaidTransaction.TransactionId] = transaction;
            return TransactionUpsertResult.Added;
        }

        existingTransaction.AccountId = account.Id;
        existingTransaction.Date = plaidTransaction.Date.Value;
        existingTransaction.AuthorizedDate = plaidTransaction.AuthorizedDate;
        existingTransaction.Name = name;
        existingTransaction.MerchantName = plaidTransaction.MerchantName;
        existingTransaction.Amount = Convert.ToDecimal(plaidTransaction.Amount);
        existingTransaction.IsoCurrencyCode = plaidTransaction.IsoCurrencyCode;
        existingTransaction.Pending = plaidTransaction.Pending ?? false;
        existingTransaction.UpdatedAt = now;

        if (existingTransaction.CategoryId is null)
        {
            existingTransaction.CategoryId = await _transactionCategorizationService
                .GetCategoryIdForPlaidTransactionAsync(
                    plaidTransaction,
                    cancellationToken);
        }

        return TransactionUpsertResult.Modified;
    }

    private async Task CategorizeUncategorizedForPlaidItemAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken)
    {
        var uncategorized = await _dbContext.Transactions
            .Where(x => x.CategoryId == null && x.Account.PlaidItemId == plaidItemId)
            .ToListAsync(cancellationToken);

        if (uncategorized.Count == 0)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();

        foreach (var transaction in uncategorized)
        {
            var categoryId = await _transactionCategorizationService
                .GetCategoryIdForStoredTransactionAsync(
                    transaction.Name,
                    transaction.MerchantName,
                    transaction.Amount,
                    cancellationToken);

            if (!categoryId.HasValue)
            {
                continue;
            }

            transaction.CategoryId = categoryId;
            transaction.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void CountUpsert(
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
