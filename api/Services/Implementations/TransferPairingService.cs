using Cardui.Api.Data;
using Cardui.Api.Models;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class TransferPairingService : ITransferPairingService
{
    private const string TransfersCategoryKey = "transfers";
    private const int LookbackDays = 120;
    private const int MaxDateSkewDays = 1;

    private static readonly HashSet<string> EligibleAccountTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "depository",
        "investment"
    };

    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TransferPairingService> _logger;

    public TransferPairingService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        ILogger<TransferPairingService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> PairOwnedAccountTransfersAsync(
        CancellationToken cancellationToken = default)
    {
        var transfersCategoryId = await _dbContext.Categories
            .AsNoTracking()
            .Where(x => x.Key == TransfersCategoryKey)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (transfersCategoryId is null)
        {
            _logger.LogWarning(
                "Skipping transfer pairing because the '{CategoryKey}' category was not found",
                TransfersCategoryKey);
            return 0;
        }

        var eligibleAccountIds = await _dbContext.Accounts
            .AsNoTracking()
            .Where(x => x.IsActive && EligibleAccountTypes.Contains(x.Type))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (eligibleAccountIds.Count < 2)
        {
            return 0;
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var windowStart = today.AddDays(-LookbackDays);

        var candidates = await _dbContext.Transactions
            .Where(x =>
                eligibleAccountIds.Contains(x.AccountId)
                && x.Date >= windowStart
                && x.Amount != 0)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (candidates.Count < 2)
        {
            return 0;
        }

        var outflows = candidates.Where(x => x.Amount > 0).ToList();
        var inflows = candidates.Where(x => x.Amount < 0).ToList();
        var usedInflowIds = new HashSet<Guid>();
        var pairCount = 0;
        var now = _timeProvider.GetUtcNow();

        foreach (var outflow in outflows)
        {
            var match = inflows.FirstOrDefault(inflow =>
                !usedInflowIds.Contains(inflow.Id)
                && inflow.AccountId != outflow.AccountId
                && inflow.Amount == -outflow.Amount
                && Math.Abs(inflow.Date.DayNumber - outflow.Date.DayNumber) <= MaxDateSkewDays);

            if (match is null)
            {
                continue;
            }

            usedInflowIds.Add(match.Id);

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

        if (pairCount > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Paired {PairCount} owned-account transfer(s) in the last {LookbackDays} days",
                pairCount,
                LookbackDays);
        }

        return pairCount;
    }
}
