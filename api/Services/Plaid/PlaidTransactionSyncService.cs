using Cardui.Api.Data;
using Cardui.Api.Dtos.Plaid;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Going.Plaid.Entity;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Plaid;

public class PlaidTransactionSyncService : IPlaidTransactionSyncService
{
    private readonly CarduiDBContext _dbContext;
    private readonly IPlaidAccessTokenProtector _accessTokenProtector;
    private readonly IPlaidTransactionPageClient _transactionPageClient;
    private readonly IPlaidTransactionReconciler _transactionReconciler;
    private readonly ITransactionCategorizationService _transactionCategorizationService;
    private readonly ITransferPairingService _transferPairingService;
    private readonly TimeProvider _timeProvider;

    public PlaidTransactionSyncService(
        CarduiDBContext dbContext,
        IPlaidAccessTokenProtector accessTokenProtector,
        IPlaidTransactionPageClient transactionPageClient,
        IPlaidTransactionReconciler transactionReconciler,
        ITransactionCategorizationService transactionCategorizationService,
        ITransferPairingService transferPairingService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _accessTokenProtector = accessTokenProtector;
        _transactionPageClient = transactionPageClient;
        _transactionReconciler = transactionReconciler;
        _transactionCategorizationService = transactionCategorizationService;
        _transferPairingService = transferPairingService;
        _timeProvider = timeProvider;
    }

    public async Task<SyncTransactionsResponseDto> SyncTransactionsForPlaidItemAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken = default)
    {
        var hasMore = true;
        var cursor = plaidItem.TransactionsCursor;
        var accessToken = _accessTokenProtector.Unprotect(plaidItem.AccessToken);
        var added = new List<Going.Plaid.Entity.Transaction>();
        var modified = new List<Going.Plaid.Entity.Transaction>();
        var removed = new List<RemovedTransaction>();

        var accountsByPlaidId = await _dbContext.Accounts
            .Where(x => x.PlaidItemId == plaidItem.Id && x.PlaidAccountId != null)
            .ToDictionaryAsync(x => x.PlaidAccountId!, cancellationToken);

        while (hasMore)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = await _transactionPageClient.GetPageAsync(
                accessToken,
                cursor,
                cancellationToken);

            added.AddRange(page.Added);
            modified.AddRange(page.Modified);
            removed.AddRange(page.Removed);

            cursor = page.NextCursor;
            hasMore = page.HasMore;
        }

        var now = _timeProvider.GetUtcNow();
        var syncResult = await _transactionReconciler.ReconcileAsync(
            plaidItem.Id,
            accountsByPlaidId,
            added,
            modified,
            removed,
            now,
            cancellationToken);

        plaidItem.TransactionsCursor = cursor;
        plaidItem.LastTransactionsSyncedAt = now;
        plaidItem.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await CategorizeUncategorizedForPlaidItemAsync(plaidItem.Id, cancellationToken);

        await _transferPairingService.PairOwnedAccountTransfersAsync(cancellationToken);

        return new SyncTransactionsResponseDto
        {
            Added = syncResult.Added,
            Modified = syncResult.Modified,
            Removed = syncResult.Removed,
            NextCursor = cursor
        };
    }

    private async Task CategorizeUncategorizedForPlaidItemAsync(
        Guid plaidItemId,
        CancellationToken cancellationToken)
    {
        var uncategorized = await _dbContext.Transactions
            .Where(x =>
                x.CategoryId == null
                && !x.IsCategoryUserEdited
                && x.Account.PlaidItemId == plaidItemId)
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

}
