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

    public async Task<int> PairOwnedAccountTransfersAsync(
        CancellationToken cancellationToken = default)
    {
        var categoryIdsByKey = await _dbContext.Categories
            .AsNoTracking()
            .Where(x =>
                x.Key == SystemCategoryKeys.Transfers
                || x.Key == SystemCategoryKeys.Other
                || x.Key == SystemCategoryKeys.Income)
            .ToDictionaryAsync(x => x.Key, x => x.Id, cancellationToken);

        if (!categoryIdsByKey.TryGetValue(SystemCategoryKeys.Transfers, out var transfersCategoryId))
        {
            _logger.LogWarning(
                "Skipping transfer pairing because the '{CategoryKey}' category was not found",
                SystemCategoryKeys.Transfers);
            return 0;
        }

        categoryIdsByKey.TryGetValue(SystemCategoryKeys.Other, out var uncategorizedCategoryId);
        categoryIdsByKey.TryGetValue(SystemCategoryKeys.Income, out var incomeCategoryId);

        var eligibleAccountIds = await _dbContext.Accounts
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(x =>
                x.IsActive
                && (x.Type == AccountTypes.Depository || x.Type == AccountTypes.Investment))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (eligibleAccountIds.Count == 0)
        {
            return 0;
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var windowStart = today.AddDays(-LookbackDays);

        var candidates = await _dbContext.Transactions
            .InHousehold(_householdScope)
            .Include(x => x.Category)
            .Where(x =>
                eligibleAccountIds.Contains(x.AccountId)
                && x.Date >= windowStart
                && x.Amount != 0)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

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

    private static bool HasTransferPairSignal(Transaction left, Transaction right)
    {
        return TransferTextClassifier.LooksLikeTransferPairSignal(left.MerchantName, left.Name)
            || TransferTextClassifier.LooksLikeTransferPairSignal(right.MerchantName, right.Name);
    }
}
