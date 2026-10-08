using Cardui.Api.Data;
using Cardui.Api.Domain.Living;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Living;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Exceptions;
using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Implementations;

public class LivingService : ILivingService
{
    private readonly CarduiDBContext _dbContext;
    private readonly IIncomeSourcesService _incomeSourcesService;
    private readonly IObligationsService _obligationsService;
    private readonly IDebtsService _debtsService;
    private readonly ISavingsGoalsService _savingsGoalsService;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public LivingService(
        CarduiDBContext dbContext,
        IIncomeSourcesService incomeSourcesService,
        IObligationsService obligationsService,
        IDebtsService debtsService,
        ISavingsGoalsService savingsGoalsService,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _dbContext = dbContext;
        _incomeSourcesService = incomeSourcesService;
        _obligationsService = obligationsService;
        _debtsService = debtsService;
        _savingsGoalsService = savingsGoalsService;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<LivingPageDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var contributors = await LoadContributorsAsync(cancellationToken);
        var incomes = await _incomeSourcesService.GetOutlookIncomesAsync(cancellationToken);
        var goals = await _savingsGoalsService.GetGoalsAsync(cancellationToken);
        var accounts = await _savingsGoalsService.GetAccountsAsync(cancellationToken);
        var bills = await _obligationsService.GetOutlookBillsAsync(cancellationToken);
        var debts = await _debtsService.GetDebtsAsync(cancellationToken);
        return Compose(contributors, incomes, goals, accounts, bills, debts);
    }

    /// <inheritdoc />
    public async Task<LivingPageDto> SetContributionAsync(
        Guid contributorId,
        UpsertLivingContributionDto dto,
        CancellationToken cancellationToken = default)
    {
        var amount = RequireContribution(dto.MonthlyAmount);
        await SaveContributionAsync(contributorId, amount, cancellationToken);
        return await GetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HouseholdIncome>> GetSharedIncomesAsync(
        IReadOnlyList<HouseholdIncome> incomes,
        CancellationToken cancellationToken = default)
    {
        var contributors = await LoadContributorsAsync(cancellationToken);
        return ContributionShare.Apply(incomes, ShareInputs(contributors), _householdScope.PlanningCurrency).Incomes;
    }

    #region Private Methods

    /// <summary>
    /// Builds the page from facts already loaded.
    /// Contribution shares and the monthly gap are pure rules.
    /// </summary>
    private LivingPageDto Compose(
        IReadOnlyList<HouseholdContributor> contributors,
        IReadOnlyList<HouseholdIncome> incomes,
        IReadOnlyList<SavingsGoalDto> goals,
        IReadOnlyList<SavingsAccountDto> accounts,
        IReadOnlyList<DatedBill> bills,
        IReadOnlyList<DebtDto> debts)
    {
        var currency = _householdScope.PlanningCurrency;
        var share = ContributionShare.Apply(incomes, ShareInputs(contributors), currency);
        var livingSpending = goals.FirstOrDefault(goal => goal.Kind == SavingsGoalKind.Operating);
        var gap = LivingGapCalculator.Measure(
            share.SharedMonthly,
            bills,
            debts.Select(debt => new LivingDebtMinimum(debt.Name, debt.MinimumPayment, debt.Currency)).ToList(),
            LivingSpendingAmount(livingSpending),
            currency);
        return LivingPageMapper.Map(currency, share, livingSpending, accounts, gap);
    }

    /// <summary>
    /// Returns the one monthly living-spending amount.
    /// A missing amount returns an empty list so the gap counts zero.
    /// </summary>
    private static IReadOnlyList<LivingSpendingAmount> LivingSpendingAmount(SavingsGoalDto? livingSpending)
    {
        return livingSpending?.MonthlyAmount is decimal monthly && monthly > 0
            ? [new LivingSpendingAmount(livingSpending.Name, monthly, livingSpending.Currency)]
            : [];
    }

    /// <summary>
    /// The current monthly benchmarks the share rule reads. The contributor order is the page order.
    /// </summary>
    private static List<ContributionShareInput> ShareInputs(IReadOnlyList<HouseholdContributor> contributors)
    {
        return contributors
            .Select(contributor => new ContributionShareInput(
                contributor.Id,
                contributor.Name,
                contributor.MonthlyContribution))
            .ToList();
    }

    /// <summary>
    /// Rejects a contribution the household cannot store.
    /// </summary>
    private static decimal? RequireContribution(decimal? amount)
    {
        if (!LivingContributionRules.TryNormalize(amount, out var stored, out var error))
        {
            throw new BadRequestException(error);
        }

        return stored;
    }

    /// <summary>
    /// Loads contributors for the signed-in household, by name.
    /// The rows are not tracked. A save loads the one row it changes.
    /// </summary>
    private async Task<List<HouseholdContributor>> LoadContributorsAsync(CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        return await _dbContext.HouseholdContributors
            .AsNoTracking()
            .Where(contributor => contributor.HouseholdId == householdId)
            .OrderBy(contributor => contributor.Name)
            .ThenBy(contributor => contributor.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Stores the contribution on the household's contributor.
    /// A missing person is not found. The amount is already checked.
    /// </summary>
    private async Task SaveContributionAsync(
        Guid contributorId,
        decimal? amount,
        CancellationToken cancellationToken)
    {
        var householdId = _householdScope.RequireHouseholdId();
        var contributor = await _dbContext.HouseholdContributors
            .FirstOrDefaultAsync(
                item => item.Id == contributorId && item.HouseholdId == householdId,
                cancellationToken);
        if (contributor is null)
        {
            throw new NotFoundException("That contributor was not found.");
        }

        contributor.MonthlyContribution = amount;
        contributor.UpdatedAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
