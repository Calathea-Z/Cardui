using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class DebtsService : IDebtsService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public DebtsService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DebtDto>> GetDebtsAsync(
        CancellationToken cancellationToken = default)
    {
        return await LoadDebtsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DebtDto> CreateAsync(
        UpsertDebtDto dto,
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var draft = RequireDraft(dto);
        var currency = await RequirePlanningCurrencyAsync(householdId, cancellationToken);
        await RequireAccountAsync(householdId, draft.AccountId, cancellationToken);
        await RequireUniqueNameAsync(householdId, draft.Name, null, cancellationToken);
        var debtId = await SaveNewDebtAsync(householdId, currency, draft, cancellationToken);
        return await ProjectDebtAsync(debtId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DebtDto> UpdateAsync(
        Guid debtId,
        UpsertDebtDto dto,
        CancellationToken cancellationToken = default)
    {
        var debt = await FindDebtAsync(debtId, cancellationToken);
        var draft = RequireDraft(dto);
        await RequireAccountAsync(debt.HouseholdId, draft.AccountId, cancellationToken);
        await RequireUniqueNameAsync(debt.HouseholdId, draft.Name, debt.Id, cancellationToken);
        await SaveDebtAsync(debt, draft, cancellationToken);
        return await ProjectDebtAsync(debt.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        Guid debtId,
        CancellationToken cancellationToken = default)
    {
        var debt = await FindDebtAsync(debtId, cancellationToken);
        await DeleteDebtAsync(debt, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DebtSummaryReportDto> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var inputs = await LoadSummaryInputsAsync(cancellationToken);
        var report = DebtSummary.Calculate(inputs, Today());
        return DebtSummaryDtoMapper.Map(report);
    }

    /// <inheritdoc />
    public async Task<DebtDto> UseAccountBalanceAsync(
        Guid debtId,
        CancellationToken cancellationToken = default)
    {
        var debt = await FindDebtAsync(debtId, cancellationToken);
        var chosen = await ReadChosenBalanceAsync(debt, cancellationToken);
        await SaveChosenBalanceAsync(debt, chosen.Balance, chosen.AsOf, cancellationToken);
        return await ProjectDebtAsync(debt.Id, cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Reads the household planning currency so a new debt is labeled in that currency.
    /// </summary>
    private async Task<string> RequirePlanningCurrencyAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        var currency = await _dbContext.Households
            .AsNoTracking()
            .Where(household => household.Id == householdId)
            .Select(household => household.PlanningCurrency)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new NotFoundException("No household exists for the signed-in owner.");
        }

        return currency;
    }

    /// <summary>
    /// Loads one debt in the signed-in household.
    /// A debt from another household is not found.
    /// </summary>
    private async Task<Debt> FindDebtAsync(
        Guid debtId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var debt = await _dbContext.Debts
            .FirstOrDefaultAsync(
                x => x.Id == debtId && x.HouseholdId == householdId,
                cancellationToken);

        if (debt is null)
        {
            throw new NotFoundException("That debt was not found.");
        }

        return debt;
    }

    /// <summary>
    /// Rejects an account that is not in this household.
    /// A missing account means the debt is not linked to one. An archived account may stay linked.
    /// </summary>
    private async Task RequireAccountAsync(
        Guid householdId,
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (accountId is not Guid id)
        {
            return;
        }

        var exists = await _dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(
                account => account.Id == id && account.HouseholdId == householdId,
                cancellationToken);

        if (!exists)
        {
            throw new BadRequestException("Choose an account from this household.");
        }
    }

    /// <summary>
    /// Rejects a name another debt in the household already uses.
    /// The comparison ignores letter case.
    /// </summary>
    private async Task RequireUniqueNameAsync(
        Guid householdId,
        string name,
        Guid? exceptDebtId,
        CancellationToken cancellationToken)
    {
        var debts = _dbContext.Debts
            .AsNoTracking()
            .Where(debt => debt.HouseholdId == householdId);

        if (exceptDebtId is Guid exceptId)
        {
            debts = debts.Where(debt => debt.Id != exceptId);
        }

        var names = await debts
            .Select(debt => debt.Name)
            .ToListAsync(cancellationToken);

        if (names.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BadRequestException("That debt is already in the household.");
        }
    }

    /// <summary>
    /// Inserts one debt and returns its id.
    /// Currency is the household planning currency at creation. Unknown terms stay null.
    /// </summary>
    private async Task<Guid> SaveNewDebtAsync(
        Guid householdId,
        string currency,
        DebtDraft draft,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var debt = new Debt
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Name = draft.Name,
            Kind = draft.Kind,
            AccountId = draft.AccountId,
            Balance = draft.Balance,
            BalanceAsOf = draft.BalanceAsOf,
            Currency = currency,
            Apr = draft.Apr,
            MinimumPayment = draft.MinimumPayment,
            NextDueDate = draft.NextDueDate,
            CreditLimit = draft.CreditLimit,
            RemainingTermMonths = draft.RemainingTermMonths,
            PromotionalApr = draft.PromotionalApr,
            PromotionalEndsOn = draft.PromotionalEndsOn,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Debts.Add(debt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return debt.Id;
    }

    /// <summary>
    /// Writes the accepted debt facts onto an existing row.
    /// Currency and the created time stay. A null term replaces a previously known one.
    /// </summary>
    private async Task SaveDebtAsync(
        Debt debt,
        DebtDraft draft,
        CancellationToken cancellationToken)
    {
        debt.Name = draft.Name;
        debt.Kind = draft.Kind;
        debt.AccountId = draft.AccountId;
        debt.Balance = draft.Balance;
        debt.BalanceAsOf = draft.BalanceAsOf;
        debt.Apr = draft.Apr;
        debt.MinimumPayment = draft.MinimumPayment;
        debt.NextDueDate = draft.NextDueDate;
        debt.CreditLimit = draft.CreditLimit;
        debt.RemainingTermMonths = draft.RemainingTermMonths;
        debt.PromotionalApr = draft.PromotionalApr;
        debt.PromotionalEndsOn = draft.PromotionalEndsOn;
        debt.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes one debt. The linked account stays.
    /// </summary>
    private async Task DeleteDebtAsync(
        Debt debt,
        CancellationToken cancellationToken)
    {
        _dbContext.Debts.Remove(debt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the API shape of one household debt, including the linked account name.
    /// Utilization is calculated after the row is read. It is not a stored column.
    /// </summary>
    private async Task<DebtDto> ProjectDebtAsync(
        Guid debtId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var debt = await _dbContext.Debts
            .AsNoTracking()
            .Where(x => x.Id == debtId && x.HouseholdId == householdId)
            .Select(DebtDtoMapper.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        if (debt is null)
        {
            throw new NotFoundException("That debt was not found.");
        }

        return WithUtilization(debt);
    }

    /// <summary>
    /// Lists the household's debts, ordered by name.
    /// </summary>
    private async Task<List<DebtDto>> LoadDebtsAsync(
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var debts = await _dbContext.Debts
            .AsNoTracking()
            .Where(debt => debt.HouseholdId == householdId)
            .OrderBy(debt => debt.Name)
            .Select(DebtDtoMapper.Projection)
            .ToListAsync(cancellationToken);

        return debts.Select(WithUtilization).ToList();
    }

    /// <summary>
    /// Adds the calculated utilization. A missing balance or credit limit stays unknown.
    /// </summary>
    private static DebtDto WithUtilization(DebtDto debt)
    {
        debt.Utilization = DebtRules.Utilization(debt.Balance, debt.CreditLimit);
        return debt;
    }

    /// <summary>
    /// Today's date in the household time zone.
    /// A due date and a promotion are compared with this day.
    /// </summary>
    private DateOnly Today()
    {
        return FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
    }

    /// <summary>
    /// Loads each debt with the latest balance of its linked account, when it has one.
    /// An account that is not linked is omitted. The debt balance is not replaced here.
    /// </summary>
    private async Task<List<DebtSummaryInput>> LoadSummaryInputsAsync(
        CancellationToken cancellationToken)
    {
        var debts = await LoadDebtsAsync(cancellationToken);
        var accountIds = debts
            .Where(debt => debt.AccountId is Guid)
            .Select(debt => debt.AccountId!.Value)
            .Distinct()
            .ToList();
        var links = await LoadLinkedBalancesAsync(accountIds, cancellationToken);

        return debts
            .Select(debt => ToSummaryInput(
                debt,
                debt.AccountId is Guid accountId && links.TryGetValue(accountId, out var linked)
                    ? linked
                    : null))
            .ToList();
    }

    /// <summary>
    /// Loads the dated balance for each linked account.
    /// The latest snapshot is the dated figure. Without one, the current balance is kept and its date stays unknown.
    /// </summary>
    private async Task<Dictionary<Guid, DebtLinkedBalance>> LoadLinkedBalancesAsync(
        IReadOnlyList<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        if (accountIds.Count == 0)
        {
            return [];
        }

        var householdId = _householdScope.RequireHouseholdId();
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.HouseholdId == householdId && accountIds.Contains(account.Id))
            .Select(account => new
            {
                account.Id,
                account.Type,
                account.CurrentBalance,
                account.IsoCurrencyCode
            })
            .ToListAsync(cancellationToken);

        var snapshots = await _dbContext.AccountBalanceSnapshots
            .AsNoTracking()
            .Where(snapshot => accountIds.Contains(snapshot.AccountId))
            .Select(snapshot => new
            {
                snapshot.AccountId,
                snapshot.Date,
                snapshot.CurrentBalance,
                snapshot.IsoCurrencyCode,
                snapshot.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var latestByAccount = snapshots
            .GroupBy(snapshot => snapshot.AccountId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(snapshot => snapshot.Date)
                    .ThenByDescending(snapshot => snapshot.CreatedAt)
                    .First());

        var links = new Dictionary<Guid, DebtLinkedBalance>();
        foreach (var account in accounts)
        {
            latestByAccount.TryGetValue(account.Id, out var snapshot);
            links[account.Id] = ToLinkedBalance(
                account.Type,
                account.CurrentBalance,
                account.IsoCurrencyCode,
                snapshot?.Date,
                snapshot?.CurrentBalance,
                snapshot?.IsoCurrencyCode);
        }

        return links;
    }

    /// <summary>
    /// Reads the account balance the person chose, or rejects a balance the debt cannot store.
    /// </summary>
    private async Task<(decimal Balance, DateOnly AsOf)> ReadChosenBalanceAsync(
        Debt debt,
        CancellationToken cancellationToken)
    {
        if (debt.AccountId is not Guid accountId)
        {
            throw new BadRequestException("This debt is not linked to an account.");
        }

        var householdId = _householdScope.RequireHouseholdId();
        var account = await _dbContext.Accounts
            .AsNoTracking()
            .Where(row => row.Id == accountId && row.HouseholdId == householdId)
            .Select(row => new
            {
                row.Type,
                row.CurrentBalance,
                row.IsoCurrencyCode
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            throw new BadRequestException("Choose an account from this household.");
        }

        var snapshot = await _dbContext.AccountBalanceSnapshots
            .AsNoTracking()
            .Where(row => row.AccountId == accountId)
            .OrderByDescending(row => row.Date)
            .ThenByDescending(row => row.CreatedAt)
            .Select(row => new
            {
                row.Date,
                row.CurrentBalance,
                row.IsoCurrencyCode
            })
            .FirstOrDefaultAsync(cancellationToken);

        var linked = ToLinkedBalance(
            account.Type,
            account.CurrentBalance,
            account.IsoCurrencyCode,
            snapshot?.Date,
            snapshot?.CurrentBalance,
            snapshot?.IsoCurrencyCode);

        if (!DebtSummary.TryReadChosenBalance(
                debt.Currency,
                linked,
                out var balance,
                out var asOf,
                out var error))
        {
            throw new BadRequestException(error);
        }

        return (balance, asOf);
    }

    /// <summary>
    /// Stores the chosen balance and the date it was true.
    /// APR, minimum, due date, and the account row stay unchanged.
    /// </summary>
    private async Task SaveChosenBalanceAsync(
        Debt debt,
        decimal balance,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {
        debt.Balance = balance;
        debt.BalanceAsOf = asOf;
        debt.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Builds one summary input from a debt and the balance of its linked account.
    /// </summary>
    private static DebtSummaryInput ToSummaryInput(DebtDto debt, DebtLinkedBalance? linked)
    {
        return new DebtSummaryInput(
            debt.Id,
            debt.Currency,
            debt.Kind,
            debt.Balance,
            debt.BalanceAsOf,
            debt.Apr,
            debt.MinimumPayment,
            debt.NextDueDate,
            debt.CreditLimit,
            debt.RemainingTermMonths,
            debt.PromotionalApr,
            debt.PromotionalEndsOn,
            linked);
    }

    /// <summary>
    /// Prefers the latest snapshot, which has a date. The current balance is used only when no snapshot exists.
    /// </summary>
    private static DebtLinkedBalance ToLinkedBalance(
        string accountType,
        decimal currentBalance,
        string? accountCurrency,
        DateOnly? snapshotDate,
        decimal? snapshotBalance,
        string? snapshotCurrency)
    {
        var hasSnapshot = snapshotDate is not null && snapshotBalance is not null;
        return new DebtLinkedBalance(
            hasSnapshot ? snapshotBalance!.Value : currentBalance,
            hasSnapshot ? snapshotDate : null,
            hasSnapshot ? snapshotCurrency ?? accountCurrency : accountCurrency,
            AccountLedger.IsLiability(accountType));
    }

    /// <summary>
    /// Turns the request into stored debt facts, or rejects it.
    /// </summary>
    private static DebtDraft RequireDraft(UpsertDebtDto dto)
    {
        if (!DebtRules.TryNormalize(
                dto.Name,
                dto.Kind,
                dto.AccountId,
                dto.Balance,
                dto.BalanceAsOf,
                dto.Apr,
                dto.MinimumPayment,
                dto.NextDueDate,
                dto.CreditLimit,
                dto.RemainingTermMonths,
                dto.PromotionalApr,
                dto.PromotionalEndsOn,
                out var draft,
                out var error))
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    #endregion
}
