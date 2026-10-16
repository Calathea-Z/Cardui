using Cardui.Api.Data;
using Cardui.Api.Domain.Allocation;
using Cardui.Api.Dtos.Allocation;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class AllocationService : IAllocationService
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public AllocationService(
        CarduiDBContext dbContext,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<AllocationPageDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var preference = await LoadPreferenceAsync(cancellationToken);
        var payments = await LoadPaymentsAsync(cancellationToken);
        return Compose(preference.Draft, preference.IsSaved, payments);
    }

    /// <inheritdoc />
    public async Task<AllocationPageDto> SaveAsync(
        UpsertAllocationPreferenceDto dto,
        CancellationToken cancellationToken = default)
    {
        var draft = RequireDraft(dto);
        await SavePreferenceAsync(draft, cancellationToken);
        return await GetAsync(cancellationToken);
    }

    #region Private Methods

    /// <summary>
    /// Builds the page from the stored percents and the income columns already loaded.
    /// Variable sources are reviewed. The split itself stays with the page that shows it.
    /// </summary>
    private AllocationPageDto Compose(
        AllocationPreferenceDraft draft,
        bool isSaved,
        IReadOnlyList<VariableIncomePayment> payments)
    {
        var reviews = AllocationPreferenceRules.Review(payments, _householdScope.PlanningCurrency);
        return new AllocationPageDto
        {
            PlanningCurrency = _householdScope.PlanningCurrency,
            IsSaved = isSaved,
            VariableIncome = Rule(
                draft.VariableIncomeDebtPercent,
                draft.VariableIncomeSavingsPercent),
            Windfall = Rule(draft.WindfallDebtPercent, draft.WindfallSavingsPercent),
            VariableIncomeReviews = reviews
                .Select(review => new VariableIncomeReviewDto
                {
                    Name = review.Name,
                    Currency = review.Currency,
                    Surplus = review.Surplus,
                    LeftOut = review.LeftOut
                })
                .ToList()
        };
    }

    /// <summary>
    /// One rule's stored percents, including the share that stays available.
    /// </summary>
    private static AllocationRuleDto Rule(int debtPercent, int savingsPercent)
    {
        return new AllocationRuleDto
        {
            DebtPercent = debtPercent,
            SavingsPercent = savingsPercent,
            CashPercent = AllocationPreferenceRules.CashPercent(debtPercent, savingsPercent)
        };
    }

    /// <summary>
    /// Rejects percents the household cannot store.
    /// </summary>
    private static AllocationPreferenceDraft RequireDraft(UpsertAllocationPreferenceDto dto)
    {
        if (!AllocationPreferenceRules.TryNormalize(
                dto.VariableIncomeDebtPercent,
                dto.VariableIncomeSavingsPercent,
                dto.WindfallDebtPercent,
                dto.WindfallSavingsPercent,
                out var draft,
                out var error)
            || draft is null)
        {
            throw new BadRequestException(error);
        }

        return draft;
    }

    /// <summary>
    /// Loads the saved percents without tracking the row.
    /// A missing row is unsaved and leaves both rules at zero.
    /// </summary>
    private async Task<(AllocationPreferenceDraft Draft, bool IsSaved)> LoadPreferenceAsync(
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var stored = await _dbContext.AllocationPreferences
            .AsNoTracking()
            .Where(preference => preference.HouseholdId == householdId)
            .Select(preference => new AllocationPreferenceDraft(
                preference.VariableIncomeDebtPercent,
                preference.VariableIncomeSavingsPercent,
                preference.WindfallDebtPercent,
                preference.WindfallSavingsPercent))
            .SingleOrDefaultAsync(cancellationToken);

        return stored is null
            ? (new AllocationPreferenceDraft(0, 0, 0, 0), false)
            : (stored, true);
    }

    /// <summary>
    /// Loads the income columns the variable-income review needs.
    /// The rows are not tracked. Name order matches the page.
    /// </summary>
    private async Task<List<VariableIncomePayment>> LoadPaymentsAsync(CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        return await _dbContext.IncomeSources
            .AsNoTracking()
            .Where(source => source.HouseholdId == householdId)
            .OrderBy(source => source.Name)
            .ThenBy(source => source.Id)
            .Select(source => new VariableIncomePayment(
                source.Name,
                source.Currency,
                source.Reliability,
                source.TakeHomeAmount,
                source.StrongTakeHomeAmount))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts or updates the household's one preference row.
    /// The draft is already checked. No balance, debt, or transaction changes.
    /// </summary>
    private async Task SavePreferenceAsync(
        AllocationPreferenceDraft draft,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var preference = await _dbContext.AllocationPreferences
            .SingleOrDefaultAsync(
                item => item.HouseholdId == householdId,
                cancellationToken);

        if (preference is null)
        {
            preference = new AllocationPreference
            {
                HouseholdId = householdId
            };
            _dbContext.AllocationPreferences.Add(preference);
        }

        preference.VariableIncomeDebtPercent = draft.VariableIncomeDebtPercent;
        preference.VariableIncomeSavingsPercent = draft.VariableIncomeSavingsPercent;
        preference.WindfallDebtPercent = draft.WindfallDebtPercent;
        preference.WindfallSavingsPercent = draft.WindfallSavingsPercent;
        preference.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
