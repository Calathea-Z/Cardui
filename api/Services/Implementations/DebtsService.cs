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
        if (debt.AccountFollowedSince is not null)
        {
            throw new BadRequestException(
                "This debt follows that account, so its balance is not copied here.");
        }

        if (debt.AccountId is Guid accountId
            && await CanFollowLinkedAccountAsync(debt, accountId, cancellationToken))
        {
            await SaveFollowAsync(debt, accountId, keepOwnBalance: false, cancellationToken);
            return await ProjectDebtAsync(debt.Id, cancellationToken);
        }

        var chosen = await ReadChosenBalanceAsync(debt, cancellationToken);
        await SaveChosenBalanceAsync(debt, chosen.Balance, chosen.AsOf, cancellationToken);
        return await ProjectDebtAsync(debt.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DebtFollowAccountDto>> GetFollowAccountsAsync(
        Guid debtId,
        CancellationToken cancellationToken = default)
    {
        var debt = await FindDebtAsync(debtId, cancellationToken);
        var accounts = await LoadEligibleAccountsAsync(debt, cancellationToken);
        return accounts.Select(account => ToFollowAccount(debt, account)).ToList();
    }

    /// <inheritdoc />
    public async Task<DebtDto> FollowAccountAsync(
        Guid debtId,
        FollowDebtAccountDto dto,
        CancellationToken cancellationToken = default)
    {
        var debt = await FindDebtAsync(debtId, cancellationToken);
        RequireNotFollowing(debt, dto.AccountId);
        var account = await RequireFollowAccountAsync(debt, dto.AccountId, cancellationToken);
        var resolution = DebtFollowedBalance.Resolve(PreviewFacts(debt, account));
        var keepOwnBalance = dto.KeepOwnBalance
            && DebtFollowedBalance.RecordedDiffers(debt.Balance, resolution.Balance);
        await SaveFollowAsync(debt, account.Id, keepOwnBalance, cancellationToken);
        return await ProjectDebtAsync(debt.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DebtDto> StopFollowingAsync(
        Guid debtId,
        CancellationToken cancellationToken = default)
    {
        var debt = await FindDebtAsync(debtId, cancellationToken);
        RequireFollowing(debt);
        var account = await FindFollowAccountAsync(debt.AccountId, cancellationToken);
        var resolution = DebtFollowedBalance.Resolve(Facts(ToRow(debt), account));
        await SaveStopAsync(debt, resolution, cancellationToken);
        return await ProjectDebtAsync(debt.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DebtDto> SetOverrideAsync(
        Guid debtId,
        DebtSyncedField field,
        SetDebtBalanceOverrideDto dto,
        CancellationToken cancellationToken = default)
    {
        var debt = await FindDebtAsync(debtId, cancellationToken);
        RequireFollowing(debt);
        RequireBalanceField(field);
        if (!DebtRules.TryReadBalanceOverride(
                dto.Balance,
                dto.BalanceAsOf,
                Today(),
                out var amount,
                out var asOf,
                out var error))
        {
            throw new BadRequestException(error);
        }

        await SaveBalanceOverrideAsync(debt, amount, asOf, cancellationToken);
        return await ProjectDebtAsync(debt.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DebtDto> ClearOverrideAsync(
        Guid debtId,
        DebtSyncedField field,
        CancellationToken cancellationToken = default)
    {
        var debt = await FindDebtAsync(debtId, cancellationToken);
        RequireFollowing(debt);
        RequireBalanceField(field);
        await ClearBalanceOverrideAsync(debt, cancellationToken);
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
    /// While following, the balance and the linked account are left alone.
    /// </summary>
    private async Task SaveDebtAsync(
        Debt debt,
        DebtDraft draft,
        CancellationToken cancellationToken)
    {
        var following = debt.AccountFollowedSince is not null;
        if (following && draft.AccountId != debt.AccountId)
        {
            throw new BadRequestException("Stop following before changing the linked account.");
        }

        debt.Name = draft.Name;
        debt.Kind = draft.Kind;
        if (!following)
        {
            debt.AccountId = draft.AccountId;
            debt.Balance = draft.Balance;
            debt.BalanceAsOf = draft.BalanceAsOf;
        }

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
        var rows = await QueryDebtsAsync(debtId, cancellationToken);
        if (rows.Count == 0)
        {
            throw new NotFoundException("That debt was not found.");
        }

        var debts = await ApplyFollowAsync(rows, cancellationToken);
        return debts[0];
    }

    /// <summary>
    /// Lists the household's debts, ordered by name.
    /// A followed balance is resolved after the rows are read. Sync does not write it.
    /// </summary>
    private async Task<List<DebtDto>> LoadDebtsAsync(
        CancellationToken cancellationToken)
    {
        var rows = await QueryDebtsAsync(null, cancellationToken);
        return await ApplyFollowAsync(rows, cancellationToken);
    }

    /// <summary>
    /// Adds the calculated utilization. A missing balance or credit limit stays unknown.
    /// The balance in use is the followed balance when the debt follows an account.
    /// </summary>
    private static DebtDto WithUtilization(DebtDto debt)
    {
        debt.Utilization = DebtRules.Utilization(debt.BalanceInUse, debt.CreditLimit);
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
            debt.BalanceInUse,
            debt.BalanceInUseAsOf,
            debt.Apr,
            debt.MinimumPayment,
            debt.NextDueDate,
            debt.CreditLimit,
            debt.RemainingTermMonths,
            debt.PromotionalApr,
            debt.PromotionalEndsOn,
            linked,
            debt.Following,
            debt.Freshness);
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

    /// <summary>
    /// Loads debt rows for the household, or one debt when an id is given.
    /// The followed balance is applied after this read.
    /// </summary>
    private async Task<List<DebtListRow>> QueryDebtsAsync(
        Guid? debtId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var debts = _dbContext.Debts
            .AsNoTracking()
            .Where(debt => debt.HouseholdId == householdId);
        if (debtId is Guid id)
        {
            debts = debts.Where(debt => debt.Id == id);
        }

        return await debts
            .OrderBy(debt => debt.Name)
            .Select(debt => new DebtListRow(
                debt.Id,
                debt.Name,
                debt.Kind,
                debt.AccountId,
                debt.Account == null ? null : debt.Account.Name,
                debt.Balance,
                debt.BalanceAsOf,
                debt.Currency,
                debt.Apr,
                debt.MinimumPayment,
                debt.NextDueDate,
                debt.CreditLimit,
                debt.RemainingTermMonths,
                debt.PromotionalApr,
                debt.PromotionalEndsOn,
                debt.AccountFollowedSince,
                debt.BalanceOverriddenAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves each followed balance from the account side.
    /// A debt that is not following keeps the balance stored on it.
    /// </summary>
    private async Task<List<DebtDto>> ApplyFollowAsync(
        IReadOnlyList<DebtListRow> rows,
        CancellationToken cancellationToken)
    {
        var accountIds = rows
            .Where(row => row.AccountFollowedSince is not null && row.AccountId is Guid)
            .Select(row => row.AccountId!.Value)
            .Distinct()
            .ToList();
        var accounts = await LoadAccountStatesAsync(accountIds, cancellationToken);
        return rows.Select(row => ToDto(row, accounts)).ToList();
    }

    /// <summary>
    /// Copies one debt row and its resolved balance into the API shape.
    /// </summary>
    private DebtDto ToDto(
        DebtListRow row,
        IReadOnlyDictionary<Guid, DebtFollowAccountState> accounts)
    {
        DebtFollowAccountState? account = null;
        if (row.AccountId is Guid accountId)
        {
            accounts.TryGetValue(accountId, out account);
        }

        var resolution = DebtFollowedBalance.Resolve(Facts(row, account));
        return WithUtilization(new DebtDto
        {
            Id = row.Id,
            Name = row.Name,
            Kind = row.Kind,
            AccountId = row.AccountId,
            AccountName = row.AccountName,
            Following = row.AccountFollowedSince is not null,
            Balance = row.Balance,
            BalanceAsOf = row.BalanceAsOf,
            BalanceInUse = resolution.Balance,
            BalanceInUseAsOf = resolution.AsOf,
            BalanceSource = resolution.Source,
            SyncedBalance = resolution.SyncedBalance,
            SyncedBalanceAsOf = resolution.SyncedAsOf,
            SyncedBalanceBlock = resolution.Block,
            BalanceCredit = resolution.Credit,
            Freshness = resolution.Freshness,
            SyncFailedOn = resolution.SyncFailedOn,
            Currency = row.Currency,
            Apr = row.Apr,
            MinimumPayment = row.MinimumPayment,
            NextDueDate = row.NextDueDate,
            CreditLimit = row.CreditLimit,
            RemainingTermMonths = row.RemainingTermMonths,
            PromotionalApr = row.PromotionalApr,
            PromotionalEndsOn = row.PromotionalEndsOn
        });
    }

    /// <summary>
    /// Builds the resolver input for a stored debt.
    /// A missing account is inactive, so a followed debt whose account is gone is not current.
    /// </summary>
    private DebtBalanceFacts Facts(DebtListRow row, DebtFollowAccountState? account)
    {
        var following = row.AccountFollowedSince is not null;
        return new DebtBalanceFacts(
            following,
            row.BalanceOverriddenAt is not null,
            row.Kind,
            row.Currency,
            row.Balance,
            row.BalanceAsOf,
            following ? account?.Balance : null,
            account?.IsActive ?? false,
            account?.IsArchived ?? false,
            account?.HasPlaidItem ?? false,
            account?.LastSyncCompletedAt,
            account?.LastSyncFailedAt,
            following ? OnHouseholdDate(account?.LastSyncFailedAt) : null,
            Today());
    }

    /// <summary>
    /// Builds the resolver input for a follow that has not been saved.
    /// The override is off, so this is the balance the connection would use.
    /// </summary>
    private DebtBalanceFacts PreviewFacts(Debt debt, DebtFollowAccountState account)
    {
        return new DebtBalanceFacts(
            true,
            false,
            debt.Kind,
            debt.Currency,
            debt.Balance,
            debt.BalanceAsOf,
            account.Balance,
            account.IsActive,
            account.IsArchived,
            account.HasPlaidItem,
            account.LastSyncCompletedAt,
            account.LastSyncFailedAt,
            OnHouseholdDate(account.LastSyncFailedAt),
            Today());
    }

    /// <summary>
    /// Copies a tracked debt into the row shape the resolver reads.
    /// The account name is filled later, when the debt is projected.
    /// </summary>
    private static DebtListRow ToRow(Debt debt)
    {
        return new DebtListRow(
            debt.Id,
            debt.Name,
            debt.Kind,
            debt.AccountId,
            null,
            debt.Balance,
            debt.BalanceAsOf,
            debt.Currency,
            debt.Apr,
            debt.MinimumPayment,
            debt.NextDueDate,
            debt.CreditLimit,
            debt.RemainingTermMonths,
            debt.PromotionalApr,
            debt.PromotionalEndsOn,
            debt.AccountFollowedSince,
            debt.BalanceOverriddenAt);
    }

    /// <summary>
    /// The calendar date of an instant in the household time zone.
    /// A missing instant stays null.
    /// </summary>
    private DateOnly? OnHouseholdDate(DateTimeOffset? instant)
    {
        return instant is DateTimeOffset value
            ? FinancialDate.InTimeZone(value, _householdScope.TimeZoneId)
            : null;
    }

    /// <summary>
    /// True when the debt's linked account can be followed and no other debt follows it.
    /// A manual account, or one already followed, stays a one-time copy.
    /// </summary>
    private async Task<bool> CanFollowLinkedAccountAsync(
        Debt debt,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await AccountIsFollowedByAnotherDebtAsync(debt, accountId, cancellationToken))
        {
            return false;
        }

        var account = await FindFollowAccountAsync(accountId, cancellationToken);
        return account is not null && CanFollow(debt, account);
    }

    /// <summary>
    /// Rejects a debt that is not following. An override applies only while it follows.
    /// </summary>
    private static void RequireFollowing(Debt debt)
    {
        if (debt.AccountFollowedSince is null)
        {
            throw new BadRequestException("This debt is not following an account.");
        }
    }

    /// <summary>
    /// Rejects a field this slice does not follow. Balance is the only one.
    /// </summary>
    private static void RequireBalanceField(DebtSyncedField field)
    {
        if (field != DebtSyncedField.Balance)
        {
            throw new BadRequestException("That field cannot be overridden.");
        }
    }

    /// <summary>
    /// Rejects a second follow. The person stops following before choosing another account.
    /// </summary>
    private static void RequireNotFollowing(Debt debt, Guid accountId)
    {
        if (debt.AccountFollowedSince is null)
        {
            return;
        }

        throw new BadRequestException(
            debt.AccountId == accountId
                ? "This debt already follows that account."
                : "Stop following before choosing another account.");
    }

    /// <summary>
    /// Loads one account the debt may follow, or rejects it.
    /// An account followed by another debt, or one that is not a connected card or loan, is rejected.
    /// </summary>
    private async Task<DebtFollowAccountState> RequireFollowAccountAsync(
        Debt debt,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await AccountIsFollowedByAnotherDebtAsync(debt, accountId, cancellationToken))
        {
            throw new BadRequestException("That account is already followed by another debt.");
        }

        var account = await FindFollowAccountAsync(accountId, cancellationToken);
        if (account is null || !CanFollow(debt, account))
        {
            throw new BadRequestException("Choose a connected credit card or loan in this currency.");
        }

        return account;
    }

    /// <summary>
    /// Lists accounts this debt may follow, ordered by name.
    /// An account another debt already follows is left out.
    /// </summary>
    private async Task<List<DebtFollowAccountState>> LoadEligibleAccountsAsync(
        Debt debt,
        CancellationToken cancellationToken)
    {
        var householdId = debt.HouseholdId;
        var taken = await _dbContext.Debts
            .AsNoTracking()
            .Where(row => row.HouseholdId == householdId
                && row.Id != debt.Id
                && row.AccountFollowedSince != null
                && row.AccountId != null)
            .Select(row => row.AccountId!.Value)
            .ToListAsync(cancellationToken);
        var candidates = await _dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.HouseholdId == householdId
                && account.Source == FinancialRecordSource.Plaid
                && account.PlaidItemId != null
                && account.IsActive
                && account.ArchivedAt == null)
            .Select(account => new
            {
                account.Id,
                account.Source,
                HasPlaidItem = account.PlaidItemId != null,
                account.IsActive,
                IsArchived = account.ArchivedAt != null,
                account.Type,
                account.IsoCurrencyCode
            })
            .ToListAsync(cancellationToken);
        var ids = candidates
            .Where(account => !taken.Contains(account.Id))
            .Where(account => DebtFollowEligibility.CanFollow(
                account.Source,
                account.HasPlaidItem,
                account.IsActive,
                account.IsArchived,
                account.Type,
                account.IsoCurrencyCode,
                debt.Currency))
            .Select(account => account.Id)
            .ToList();
        var states = await LoadAccountStatesAsync(ids, cancellationToken);
        return states.Values.OrderBy(account => account.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// True when another debt in the household already follows this account.
    /// </summary>
    private async Task<bool> AccountIsFollowedByAnotherDebtAsync(
        Debt debt,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Debts
            .AsNoTracking()
            .AnyAsync(
                row => row.HouseholdId == debt.HouseholdId
                    && row.Id != debt.Id
                    && row.AccountId == accountId
                    && row.AccountFollowedSince != null,
                cancellationToken);
    }

    /// <summary>
    /// Loads one account in the household. An account from another household is missing.
    /// </summary>
    private async Task<DebtFollowAccountState?> FindFollowAccountAsync(
        Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (accountId is not Guid id)
        {
            return null;
        }

        var states = await LoadAccountStatesAsync([id], cancellationToken);
        return states.TryGetValue(id, out var state) ? state : null;
    }

    /// <summary>
    /// True when this account is a connected credit card or loan in the debt's currency.
    /// </summary>
    private static bool CanFollow(Debt debt, DebtFollowAccountState account)
    {
        return DebtFollowEligibility.CanFollow(
            account.Source,
            account.HasPlaidItem,
            account.IsActive,
            account.IsArchived,
            account.Type,
            account.Currency,
            debt.Currency);
    }

    /// <summary>
    /// Describes one account the person can choose to follow.
    /// BalanceInUse is the amount following would use before they keep their own.
    /// </summary>
    private DebtFollowAccountDto ToFollowAccount(Debt debt, DebtFollowAccountState account)
    {
        var resolution = DebtFollowedBalance.Resolve(PreviewFacts(debt, account));
        return new DebtFollowAccountDto
        {
            AccountId = account.Id,
            Name = account.Name,
            Mask = account.Mask,
            SyncedBalance = resolution.SyncedBalance,
            SyncedBalanceAsOf = resolution.SyncedAsOf,
            BalanceInUse = resolution.Balance,
            BalanceInUseAsOf = resolution.AsOf,
            Block = resolution.Block,
            BalanceCredit = resolution.Credit,
            BalancesDiffer = DebtFollowedBalance.RecordedDiffers(debt.Balance, resolution.Balance)
        };
    }

    /// <summary>
    /// Records that the debt follows the account.
    /// The stored balance stays. An override is set only when the person keeps a different amount.
    /// </summary>
    private async Task SaveFollowAsync(
        Debt debt,
        Guid accountId,
        bool keepOwnBalance,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        debt.AccountId = accountId;
        debt.AccountFollowedSince = now;
        debt.BalanceOverriddenAt = keepOwnBalance ? now : null;
        debt.UpdatedAt = now;
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsFollowedAccountTaken(exception))
        {
            throw new BadRequestException("That account is already followed by another debt.");
        }
    }

    /// <summary>
    /// True when the database rejected a second debt following the same account.
    /// </summary>
    private static bool IsFollowedAccountTaken(DbUpdateException exception)
    {
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner.Message.Contains("IX_Debts_AccountId_Followed", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Copies the last synced balance onto the debt when the person had not set their own, then clears the follow.
    /// The account link stays. The account and its snapshots are not changed.
    /// </summary>
    private async Task SaveStopAsync(
        Debt debt,
        DebtBalanceResolution resolution,
        CancellationToken cancellationToken)
    {
        if (resolution.Source == DebtFieldSource.Synced
            && resolution.Balance is decimal amount
            && resolution.AsOf is DateOnly asOf)
        {
            debt.Balance = amount;
            debt.BalanceAsOf = asOf;
        }

        debt.AccountFollowedSince = null;
        debt.BalanceOverriddenAt = null;
        debt.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stores the person's balance and marks it as an override.
    /// The account and its snapshots are not changed. Sync does not clear the mark.
    /// </summary>
    private async Task SaveBalanceOverrideAsync(
        Debt debt,
        decimal balance,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        debt.Balance = balance;
        debt.BalanceAsOf = asOf;
        debt.BalanceOverriddenAt = now;
        debt.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Clears the override mark. The stored balance stays until the debt stops following.
    /// A debt that is already using the synced balance is left as it is.
    /// </summary>
    private async Task ClearBalanceOverrideAsync(
        Debt debt,
        CancellationToken cancellationToken)
    {
        if (debt.BalanceOverriddenAt is null)
        {
            return;
        }

        debt.BalanceOverriddenAt = null;
        debt.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads the account-side values for the given accounts.
    /// The latest snapshot is the dated balance. Sync times come from the bank link, without its token.
    /// </summary>
    private async Task<Dictionary<Guid, DebtFollowAccountState>> LoadAccountStatesAsync(
        IReadOnlyCollection<Guid> accountIds,
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
                account.Name,
                account.Mask,
                account.Source,
                account.PlaidItemId,
                account.IsActive,
                account.ArchivedAt,
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
        var itemIds = accounts
            .Where(account => account.PlaidItemId is Guid)
            .Select(account => account.PlaidItemId!.Value)
            .Distinct()
            .ToList();
        var syncs = await _dbContext.PlaidItems
            .AsNoTracking()
            .Where(item => item.HouseholdId == householdId && itemIds.Contains(item.Id))
            .Select(item => new
            {
                item.Id,
                item.LastSyncCompletedAt,
                item.LastSyncFailedAt
            })
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var states = new Dictionary<Guid, DebtFollowAccountState>();
        foreach (var account in accounts)
        {
            latestByAccount.TryGetValue(account.Id, out var snapshot);
            DateTimeOffset? completedAt = null;
            DateTimeOffset? failedAt = null;
            if (account.PlaidItemId is Guid itemId && syncs.TryGetValue(itemId, out var sync))
            {
                completedAt = sync.LastSyncCompletedAt;
                failedAt = sync.LastSyncFailedAt;
            }

            states[account.Id] = new DebtFollowAccountState(
                account.Id,
                account.Name,
                account.Mask,
                account.Source,
                account.PlaidItemId is not null,
                account.IsActive,
                account.ArchivedAt is not null,
                account.Type,
                account.IsoCurrencyCode,
                ToLinkedBalance(
                    account.Type,
                    account.CurrentBalance,
                    account.IsoCurrencyCode,
                    snapshot?.Date,
                    snapshot?.CurrentBalance,
                    snapshot?.IsoCurrencyCode),
                completedAt,
                failedAt);
        }

        return states;
    }

    #endregion
}
