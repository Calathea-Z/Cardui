using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class TransferPairingService : ITransferPairingService
{
    private const int LookbackDays = 120;
    private const int MaxDateSkewDays = 1;

    private readonly CarduiDBContext _dbContext;
    private readonly ITransactionCategorizationService _categorizationService;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;
    private readonly ILogger<TransferPairingService> _logger;

    public TransferPairingService(
        CarduiDBContext dbContext,
        ITransactionCategorizationService categorizationService,
        TimeProvider timeProvider,
        HouseholdScope householdScope,
        ILogger<TransferPairingService> logger)
    {
        _dbContext = dbContext;
        _categorizationService = categorizationService;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> PairOwnedAccountTransfersAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await LoadPairingCategoriesAsync(cancellationToken);
        if (categories is null)
        {
            return 0;
        }

        var (transfersCategoryId, uncategorizedCategoryId, incomeCategoryId) = categories.Value;
        var eligibleAccountIds = await LoadEligibleAccountIdsAsync(cancellationToken);
        if (eligibleAccountIds.Count == 0)
        {
            return 0;
        }

        var candidates = await LoadCandidateTransactionsAsync(
            eligibleAccountIds,
            cancellationToken);
        if (candidates.Count == 0)
        {
            return 0;
        }

        var pairedIds = new HashSet<Guid>();
        var pairCount = 0;
        var now = _timeProvider.GetUtcNow();

        if (eligibleAccountIds.Count >= 2 && candidates.Count >= 2)
        {
            pairCount = PairOppositeLegs(
                candidates,
                uncategorizedCategoryId,
                transfersCategoryId,
                incomeCategoryId,
                pairedIds,
                now);
        }

        var promotedCount = PromoteBankTransferNamedTransactions(
            candidates,
            transfersCategoryId,
            uncategorizedCategoryId,
            incomeCategoryId,
            pairedIds,
            now);

        var repairedCount = await RepairFalsePositiveTransfersAsync(
            candidates,
            pairedIds,
            transfersCategoryId,
            now,
            cancellationToken);

        if (pairCount > 0 || promotedCount > 0 || repairedCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Transfer sync: paired {PairCount}, promoted {PromotedCount} bank transfer(s), repaired {RepairedCount} false positive(s) in the last {LookbackDays} days",
                pairCount,
                promotedCount,
                repairedCount,
                LookbackDays);
        }

        return pairCount + promotedCount;
    }

    #region Private Methods

    /// <summary>
    /// Loads the transfer, uncategorized, and income category ids.
    /// Returns null when the Transfers category is missing.
    /// </summary>
    private async Task<(Guid TransfersCategoryId, Guid UncategorizedCategoryId, Guid IncomeCategoryId)?> LoadPairingCategoriesAsync(
        CancellationToken cancellationToken)
    {
        var categoryIdsByKey = await _dbContext.Categories
            .AsNoTracking()
            .Where(category =>
                category.Key == SystemCategoryKeys.Transfers
                || category.Key == SystemCategoryKeys.Other
                || category.Key == SystemCategoryKeys.Income)
            .ToDictionaryAsync(category => category.Key, category => category.Id, cancellationToken);

        if (!categoryIdsByKey.TryGetValue(SystemCategoryKeys.Transfers, out var transfersCategoryId))
        {
            _logger.LogWarning(
                "Skipping transfer pairing because the '{CategoryKey}' category was not found",
                SystemCategoryKeys.Transfers);
            return null;
        }

        categoryIdsByKey.TryGetValue(SystemCategoryKeys.Other, out var uncategorizedCategoryId);
        categoryIdsByKey.TryGetValue(SystemCategoryKeys.Income, out var incomeCategoryId);
        return (transfersCategoryId, uncategorizedCategoryId, incomeCategoryId);
    }

    /// <summary>
    /// Loads active cash and investment accounts in the household.
    /// </summary>
    private async Task<List<Guid>> LoadEligibleAccountIdsAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(account =>
                account.IsActive
                && account.ArchivedAt == null
                && (account.Type == AccountTypes.Depository || account.Type == AccountTypes.Investment))
            .Select(account => account.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Loads non-zero transactions in the lookback window for the eligible accounts.
    /// Balance reconciliations are excluded.
    /// </summary>
    private async Task<List<Transaction>> LoadCandidateTransactionsAsync(
        IReadOnlyList<Guid> eligibleAccountIds,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var windowStart = today.AddDays(-LookbackDays);

        return await _dbContext.Transactions
            .InHousehold(_householdScope)
            .Include(transaction => transaction.Category)
            .Where(transaction =>
                eligibleAccountIds.Contains(transaction.AccountId)
                && transaction.ArchivedAt == null
                && transaction.Provenance != FinancialRecordProvenance.BalanceReconciliation
                && transaction.Date >= windowStart
                && transaction.Amount != 0)
            .OrderBy(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Links an outflow to an opposite inflow of the same amount within one day,
    /// on a different owned account, when either side looks like a transfer.
    /// Both legs are categorized as Transfers.
    /// </summary>
    private static int PairOppositeLegs(
        List<Transaction> candidates,
        Guid uncategorizedCategoryId,
        Guid transfersCategoryId,
        Guid incomeCategoryId,
        HashSet<Guid> pairedIds,
        DateTimeOffset now)
    {
        var outflows = candidates.Where(x => x.Amount > 0).ToList();
        var inflows = candidates.Where(x => x.Amount < 0).ToList();
        var usedInflowIds = new HashSet<Guid>();
        var pairCount = 0;

        foreach (var outflow in outflows)
        {
            if (!IsTransferCandidateCategory(
                    outflow.Category,
                    uncategorizedCategoryId,
                    transfersCategoryId,
                    incomeCategoryId))
            {
                continue;
            }

            var match = inflows.FirstOrDefault(inflow =>
                !usedInflowIds.Contains(inflow.Id)
                && IsTransferCandidateCategory(
                    inflow.Category,
                    uncategorizedCategoryId,
                    transfersCategoryId,
                    incomeCategoryId)
                && inflow.AccountId != outflow.AccountId
                && inflow.Amount == -outflow.Amount
                && Math.Abs(inflow.Date.DayNumber - outflow.Date.DayNumber) <= MaxDateSkewDays
                && HasTransferPairSignal(outflow, inflow));

            if (match is null)
            {
                continue;
            }

            usedInflowIds.Add(match.Id);
            pairedIds.Add(outflow.Id);
            pairedIds.Add(match.Id);

            if (outflow.CategoryId != transfersCategoryId)
            {
                outflow.CategoryId = transfersCategoryId;
                outflow.UpdatedAt = now;
            }

            if (match.CategoryId != transfersCategoryId)
            {
                match.CategoryId = transfersCategoryId;
                match.UpdatedAt = now;
            }

            pairCount++;
        }

        return pairCount;
    }

    /// <summary>
    /// Marks a single leg as a Transfer when its text is a bank-transfer phrase.
    /// Those rows are kept even if they have no opposite leg.
    /// </summary>
    private static int PromoteBankTransferNamedTransactions(
        List<Transaction> candidates,
        Guid transfersCategoryId,
        Guid uncategorizedCategoryId,
        Guid incomeCategoryId,
        HashSet<Guid> keepAsTransferIds,
        DateTimeOffset now)
    {
        var promotedCount = 0;

        foreach (var transaction in candidates)
        {
            if (!TransferTextClassifier.LooksLikeBankTransfer(
                    transaction.MerchantName,
                    transaction.Name))
            {
                continue;
            }

            if (!IsTransferCandidateCategory(
                    transaction.Category,
                    uncategorizedCategoryId,
                    transfersCategoryId,
                    incomeCategoryId))
            {
                continue;
            }

            keepAsTransferIds.Add(transaction.Id);

            if (transaction.CategoryId == transfersCategoryId)
            {
                continue;
            }

            transaction.CategoryId = transfersCategoryId;
            transaction.UpdatedAt = now;
            promotedCount++;
        }

        return promotedCount;
    }

    /// <summary>
    /// Recategorizes Transfer rows from this window that were not paired or
    /// promoted, using keyword categorization.
    /// </summary>
    private async Task<int> RepairFalsePositiveTransfersAsync(
        List<Transaction> candidates,
        HashSet<Guid> keepAsTransferIds,
        Guid transfersCategoryId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var repairedCount = 0;

        foreach (var transaction in candidates)
        {
            if (transaction.CategoryId != transfersCategoryId
                || keepAsTransferIds.Contains(transaction.Id))
            {
                continue;
            }

            var categoryId = await _categorizationService.GetCategoryIdForStoredTransactionAsync(
                transaction.Name,
                transaction.MerchantName,
                transaction.Amount,
                cancellationToken);

            if (categoryId == transfersCategoryId)
            {
                keepAsTransferIds.Add(transaction.Id);
                continue;
            }

            transaction.CategoryId = categoryId;
            transaction.UpdatedAt = now;
            repairedCount++;
        }

        return repairedCount;
    }

    /// <summary>
    /// True for an uncategorized, transfer, or income category, and for a missing category.
    /// Other categories are left alone.
    /// </summary>
    private static bool IsTransferCandidateCategory(
        Category? category,
        Guid uncategorizedCategoryId,
        Guid transfersCategoryId,
        Guid incomeCategoryId)
    {
        if (category is null)
        {
            return true;
        }

        if (category.Id == transfersCategoryId
            || category.Id == uncategorizedCategoryId
            || category.Id == incomeCategoryId)
        {
            return true;
        }

        return string.Equals(category.Key, SystemCategoryKeys.Other, StringComparison.OrdinalIgnoreCase)
            || string.Equals(category.Key, SystemCategoryKeys.Transfers, StringComparison.OrdinalIgnoreCase)
            || string.Equals(category.Key, SystemCategoryKeys.Income, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when either transaction's text contains a transfer or payment-rail hint.
    /// </summary>
    private static bool HasTransferPairSignal(Transaction left, Transaction right)
    {
        return TransferTextClassifier.LooksLikeTransferPairSignal(left.MerchantName, left.Name)
            || TransferTextClassifier.LooksLikeTransferPairSignal(right.MerchantName, right.Name);
    }

    #endregion
}
