using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Savings;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class SavingsGoalsService : ISavingsGoalsService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public SavingsGoalsService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SavingsGoalDto>> GetGoalsAsync(
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var today = Today();
        var goals = await LoadGoalsAsync(householdId, cancellationToken);
        var accounts = await LoadAccountsAsync(AccountIds(goals), cancellationToken);
        return goals
            .OrderBy(goal => KindOrder(goal.Kind))
            .ThenBy(goal => goal.Name)
            .Select(goal => SavingsGoalDtoMapper.Map(
                goal,
                AccountFor(goal, accounts),
                today,
                _householdScope.PlanningCurrency))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SavingsAccountDto>> GetAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var goals = await LoadGoalsAsync(householdId, cancellationToken);
        var followed = goals
            .Where(goal => goal.AccountFollowedSince is not null && goal.AccountId is Guid)
            .ToDictionary(goal => goal.AccountId!.Value, goal => goal.Id);
        var accounts = await LoadHouseholdAccountsAsync(cancellationToken);
        return accounts
            .Where(account => SavingsAccounts.CanFollow(
                account.Type,
                account.Currency,
                account.IsActive,
                account.ArchivedAt,
                _householdScope.PlanningCurrency))
            .OrderBy(account => account.Name)
            .Select(account => new SavingsAccountDto
            {
                Id = account.Id,
                Name = account.Name,
                Mask = account.Mask,
                Balance = account.CurrentBalance,
                Currency = account.Currency,
                FollowedByGoalId = followed.TryGetValue(account.Id, out var goalId) ? goalId : null
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<SavingsOutlook> GetOutlookAsync(
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var goals = await LoadGoalsAsync(householdId, cancellationToken);
        var accounts = await LoadAccountsAsync(AccountIds(goals), cancellationToken);
        var snapshots = goals
            .Select(goal => Snapshot(goal, AccountFor(goal, accounts)))
            .ToList();
        return HouseholdSavings.Project(today, _householdScope.PlanningCurrency, snapshots);
    }

    /// <inheritdoc />
    public async Task<SavingsGoalDto> CreateAsync(
        UpsertSavingsGoalDto dto,
        CancellationToken cancellationToken = default)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var draft = RequireDraft(dto);
        await RequireKindSlotAsync(householdId, draft.Kind, null, cancellationToken);
        await RequireUniqueNameAsync(householdId, draft, null, cancellationToken);
        var account = await RequireEligibleAccountAsync(draft.AccountId, null, cancellationToken);
        var currency = _householdScope.PlanningCurrency;
        var goalId = await SaveNewAsync(householdId, currency, draft, account, cancellationToken);
        return await ProjectAsync(goalId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SavingsGoalDto> UpdateAsync(
        Guid goalId,
        UpsertSavingsGoalDto dto,
        CancellationToken cancellationToken = default)
    {
        var goal = await FindGoalAsync(goalId, cancellationToken);
        var draft = RequireDraft(dto);
        RequireSameKind(goal, draft);
        await RequireUniqueNameAsync(goal.HouseholdId, draft, goal.Id, cancellationToken);
        var account = await RequireEligibleAccountAsync(draft.AccountId, goal.Id, cancellationToken);
        await SaveUpdateAsync(goal, draft, account, cancellationToken);
        return await ProjectAsync(goal.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        Guid goalId,
        CancellationToken cancellationToken = default)
    {
        var goal = await FindGoalAsync(goalId, cancellationToken);
        _dbContext.SavingsGoals.Remove(goal);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Today in the household time zone.
    /// </summary>
    private DateOnly Today()
    {
        return FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
    }

    /// <summary>
    /// Checks the request and returns the values to store.
    /// </summary>
    private static SavingsGoalDraft RequireDraft(UpsertSavingsGoalDto dto)
    {
        if (!SavingsGoalRules.TryNormalize(
            dto.Kind,
            dto.Name,
            dto.TargetAmount,
            dto.TargetDate,
            dto.ReservedAmount,
            dto.AccountId,
            dto.UseAccountBalance,
            dto.MonthlyAmount,
            dto.ReadyDay,
            dto.FloorAmount,
            out var draft,
            out var error))
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    /// <summary>
    /// Rejects a change of kind.
    /// Operating cash stays operating cash, and a sinking fund stays a sinking fund.
    /// </summary>
    private static void RequireSameKind(SavingsGoal goal, SavingsGoalDraft draft)
    {
        if (goal.Kind != draft.Kind)
        {
            throw new BadRequestException("A goal's kind cannot change.");
        }
    }

    /// <summary>
    /// Rejects a second operating reserve or emergency goal.
    /// Sinking funds are not limited to one.
    /// </summary>
    private async Task RequireKindSlotAsync(
        Guid householdId,
        SavingsGoalKind kind,
        Guid? exceptGoalId,
        CancellationToken cancellationToken)
    {
        if (kind == SavingsGoalKind.Sinking)
        {
            return;
        }

        var goals = _dbContext.SavingsGoals
            .AsNoTracking()
            .Where(goal => goal.HouseholdId == householdId && goal.Kind == kind);
        if (exceptGoalId is Guid exceptId)
        {
            goals = goals.Where(goal => goal.Id != exceptId);
        }

        if (await goals.AnyAsync(cancellationToken))
        {
            throw new BadRequestException(kind switch
            {
                SavingsGoalKind.Operating => "This household already has everyday spending set.",
                SavingsGoalKind.Floor => "This household already has cash to keep set.",
                _ => "This household already has an emergency goal."
            });
        }
    }

    /// <summary>
    /// Rejects a sinking fund whose name matches another sinking fund in the household.
    /// The match ignores case. Operating cash and the emergency goal keep their own names.
    /// </summary>
    private async Task RequireUniqueNameAsync(
        Guid householdId,
        SavingsGoalDraft draft,
        Guid? exceptGoalId,
        CancellationToken cancellationToken)
    {
        if (draft.Kind != SavingsGoalKind.Sinking)
        {
            return;
        }

        var goals = _dbContext.SavingsGoals
            .AsNoTracking()
            .Where(goal => goal.HouseholdId == householdId && goal.Kind == SavingsGoalKind.Sinking);
        if (exceptGoalId is Guid exceptId)
        {
            goals = goals.Where(goal => goal.Id != exceptId);
        }

        var names = await goals.Select(goal => goal.Name).ToListAsync(cancellationToken);
        if (names.Any(existing => string.Equals(existing, draft.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BadRequestException("You are already saving for that.");
        }
    }

    /// <summary>
    /// Loads the cash account a goal may follow.
    /// A missing id means the goal does not follow an account. An ineligible account is rejected.
    /// An account already followed by another goal is rejected.
    /// </summary>
    private async Task<SavingsAccountBalance?> RequireEligibleAccountAsync(
        Guid? accountId,
        Guid? exceptGoalId,
        CancellationToken cancellationToken)
    {
        if (accountId is not Guid id)
        {
            return null;
        }

        var accounts = await LoadAccountsAsync([id], cancellationToken);
        if (!accounts.TryGetValue(id, out var match)
            || !SavingsAccounts.CanFollow(
                match.Type,
                match.Currency,
                match.IsActive,
                match.ArchivedAt,
                _householdScope.PlanningCurrency))
        {
            throw new BadRequestException("Choose a cash account in this household.");
        }

        var followed = _dbContext.SavingsGoals
            .AsNoTracking()
            .Where(goal => goal.AccountId == id && goal.AccountFollowedSince != null);
        if (exceptGoalId is Guid exceptId)
        {
            followed = followed.Where(goal => goal.Id != exceptId);
        }

        var taken = await followed.AnyAsync(cancellationToken);
        if (taken)
        {
            throw new BadRequestException("That account is already followed by another goal.");
        }

        return match;
    }

    /// <summary>
    /// Inserts one goal and returns its id.
    /// Currency is the household planning currency. No transaction is written.
    /// </summary>
    private async Task<Guid> SaveNewAsync(
        Guid householdId,
        string currency,
        SavingsGoalDraft draft,
        SavingsAccountBalance? account,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var goal = new SavingsGoal
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Kind = draft.Kind,
            Name = draft.Name,
            TargetAmount = draft.TargetAmount,
            TargetDate = draft.TargetDate,
            MonthlyAmount = draft.MonthlyAmount,
            ReadyDay = draft.ReadyDay,
            FloorAmount = draft.FloorAmount,
            Currency = currency,
            CreatedAt = now,
            UpdatedAt = now
        };
        ApplyFollow(goal, draft, account, now);
        _dbContext.SavingsGoals.Add(goal);
        await SaveAsync(cancellationToken);
        return goal.Id;
    }

    /// <summary>
    /// Writes the accepted facts onto an existing goal.
    /// Currency, kind, and the created time stay. No transaction is written.
    /// </summary>
    private async Task SaveUpdateAsync(
        SavingsGoal goal,
        SavingsGoalDraft draft,
        SavingsAccountBalance? account,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        goal.Name = draft.Name;
        goal.TargetAmount = draft.TargetAmount;
        goal.TargetDate = draft.TargetDate;
        goal.MonthlyAmount = draft.MonthlyAmount;
        goal.ReadyDay = draft.ReadyDay;
        goal.FloorAmount = draft.FloorAmount;
        ApplyFollow(goal, draft, account, now);
        goal.UpdatedAt = now;
        await SaveAsync(cancellationToken);
    }

    /// <summary>
    /// Stores the follow, the override, or the typed amount.
    /// Clearing the account keeps the amount submitted with the save. Using the balance clears the override.
    /// </summary>
    private static void ApplyFollow(
        SavingsGoal goal,
        SavingsGoalDraft draft,
        SavingsAccountBalance? account,
        DateTimeOffset now)
    {
        if (account is null)
        {
            goal.ReservedAmount = draft.ReservedAmount;
            goal.AccountId = null;
            goal.AccountFollowedSince = null;
            goal.ReservedOverriddenAt = null;
            return;
        }

        var sameAccount = goal.AccountId == account.Id && goal.AccountFollowedSince is not null;
        goal.AccountId = account.Id;
        goal.AccountFollowedSince = sameAccount ? goal.AccountFollowedSince : now;
        var balance = SavingsAmount.InUse(true, false, 0, account.CurrentBalance);
        var usesBalance = draft.UseAccountBalance || draft.ReservedAmount == balance;
        if (usesBalance)
        {
            goal.ReservedAmount = balance;
            goal.ReservedOverriddenAt = null;
            return;
        }

        goal.ReservedAmount = draft.ReservedAmount;
        goal.ReservedOverriddenAt = now;
    }

    /// <summary>
    /// Saves the goal, and turns a unique-index race into the same message the earlier check uses.
    /// </summary>
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsIndex(exception, "IX_SavingsGoals_HouseholdId_Operating"))
        {
            throw new BadRequestException("This household already has everyday spending set.");
        }
        catch (DbUpdateException exception) when (IsIndex(exception, "IX_SavingsGoals_HouseholdId_Floor"))
        {
            throw new BadRequestException("This household already has cash to keep set.");
        }
        catch (DbUpdateException exception) when (IsIndex(exception, "IX_SavingsGoals_HouseholdId_Emergency"))
        {
            throw new BadRequestException("This household already has an emergency goal.");
        }
        catch (DbUpdateException exception) when (IsIndex(exception, "IX_SavingsGoals_AccountId_Followed"))
        {
            throw new BadRequestException("That account is already followed by another goal.");
        }
    }

    /// <summary>
    /// True when the database rejected a unique savings index.
    /// </summary>
    private static bool IsIndex(DbUpdateException exception, string name)
    {
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner.Message.Contains(name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Loads one goal in the signed-in household.
    /// A goal from another household is not found.
    /// </summary>
    private async Task<SavingsGoal> FindGoalAsync(
        Guid goalId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var goal = await _dbContext.SavingsGoals
            .FirstOrDefaultAsync(
                item => item.Id == goalId && item.HouseholdId == householdId,
                cancellationToken);
        if (goal is null)
        {
            throw new NotFoundException("That goal was not found.");
        }

        return goal;
    }

    /// <summary>
    /// Loads the API shape of one goal.
    /// </summary>
    private async Task<SavingsGoalDto> ProjectAsync(
        Guid goalId,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var goal = await _dbContext.SavingsGoals
            .AsNoTracking()
            .FirstAsync(
                item => item.Id == goalId && item.HouseholdId == householdId,
                cancellationToken);
        var accounts = await LoadAccountsAsync(AccountIds([goal]), cancellationToken);
        return SavingsGoalDtoMapper.Map(
            goal,
            AccountFor(goal, accounts),
            Today(),
            _householdScope.PlanningCurrency);
    }

    /// <summary>
    /// The outlook facts for one goal.
    /// The amount in use follows the same rule as the page.
    /// </summary>
    private SavingsGoalSnapshot Snapshot(SavingsGoal goal, SavingsAccountBalance? account)
    {
        return new SavingsGoalSnapshot(
            goal.Id,
            goal.Name,
            goal.Currency,
            goal.TargetAmount ?? 0,
            goal.TargetDate ?? default,
            SavingsGoalDtoMapper.AmountInUse(goal, account, _householdScope.PlanningCurrency),
            goal.Kind,
            goal.MonthlyAmount ?? 0,
            goal.ReadyDay ?? 1,
            goal.FloorAmount ?? 0);
    }

    /// <summary>
    /// Loads the household's goals.
    /// The rows are not tracked. Callers order them.
    /// </summary>
    private async Task<List<SavingsGoal>> LoadGoalsAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.SavingsGoals
            .AsNoTracking()
            .Where(goal => goal.HouseholdId == householdId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// The account ids stored on these goals.
    /// </summary>
    private static List<Guid> AccountIds(IEnumerable<SavingsGoal> goals)
    {
        return goals
            .Where(goal => goal.AccountId is Guid)
            .Select(goal => goal.AccountId!.Value)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// The loaded account for one goal, when it is still in the household.
    /// </summary>
    private static SavingsAccountBalance? AccountFor(
        SavingsGoal goal,
        IReadOnlyDictionary<Guid, SavingsAccountBalance> accounts)
    {
        return goal.AccountId is Guid id && accounts.TryGetValue(id, out var account)
            ? account
            : null;
    }

    /// <summary>
    /// Loads the named accounts in this household.
    /// The balance, type, and currency are the columns the follow rule reads.
    /// </summary>
    private async Task<Dictionary<Guid, SavingsAccountBalance>> LoadAccountsAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        if (accountIds.Count == 0)
        {
            return [];
        }

        var rows = await _dbContext.Accounts
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(account => accountIds.Contains(account.Id))
            .Select(account => new SavingsAccountBalance(
                account.Id,
                account.Name,
                account.Mask,
                account.Type,
                account.IsoCurrencyCode,
                account.CurrentBalance,
                account.IsActive,
                account.ArchivedAt))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(account => account.Id);
    }

    /// <summary>
    /// Loads the household's active accounts so the page can offer the cash ones.
    /// Archived accounts are left out before the cash check.
    /// </summary>
    private async Task<List<SavingsAccountBalance>> LoadHouseholdAccountsAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .InHousehold(_householdScope)
            .Where(account => account.IsActive && account.ArchivedAt == null)
            .Select(account => new SavingsAccountBalance(
                account.Id,
                account.Name,
                account.Mask,
                account.Type,
                account.IsoCurrencyCode,
                account.CurrentBalance,
                account.IsActive,
                account.ArchivedAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Operating cash, then the emergency goal, then sinking funds.
    /// </summary>
    private static int KindOrder(SavingsGoalKind kind)
    {
        return kind switch
        {
            SavingsGoalKind.Operating => 0,
            SavingsGoalKind.Floor => 1,
            SavingsGoalKind.Emergency => 2,
            _ => 3
        };
    }

    #endregion
}
