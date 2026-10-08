using Cardui.Api.Domain;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Mapping;
using Cardui.Api.Security;
using Cardui.Api.Services.Interfaces;

namespace Cardui.Api.Services.Implementations;

public class PlanService : IPlanService
{
    private readonly IDebtsService _debtsService;
    private readonly IIncomeSourcesService _incomeSourcesService;
    private readonly IObligationsService _obligationsService;
    private readonly IAccountsService _accountsService;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public PlanService(
        IDebtsService debtsService,
        IIncomeSourcesService incomeSourcesService,
        IObligationsService obligationsService,
        IAccountsService accountsService,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _debtsService = debtsService;
        _incomeSourcesService = incomeSourcesService;
        _obligationsService = obligationsService;
        _accountsService = accountsService;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<PlanRecoveryDto> GetRecoveryAsync(
        CancellationToken cancellationToken = default)
    {
        var today = FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
        var debts = await _debtsService.GetDebtsAsync(cancellationToken);
        var cash = await LoadCashFactsAsync(today, cancellationToken);
        return ProjectPlan(debts, cash);
    }

    #region Private Methods

    /// <summary>
    /// Loads the starting cash, income, and bills the cash outlook reads, for a forecast that starts today.
    /// The loads run one after another because they share one database context.
    /// </summary>
    private async Task<HouseholdCashOutlookInput> LoadCashFactsAsync(
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var startingCash = await _accountsService.GetCashTotalAsync(cancellationToken);
        var incomes = await _incomeSourcesService.GetOutlookIncomesAsync(cancellationToken);
        var bills = await _obligationsService.GetOutlookBillsAsync(cancellationToken);
        return new HouseholdCashOutlookInput(
            _householdScope.PlanningCurrency,
            today,
            startingCash,
            incomes,
            bills);
    }

    /// <summary>
    /// Runs the approved rollover and recovery rules from the outlook's start date, then the cash outlook on both paths,
    /// and shapes the result for the page. Extra and reclaim stay at the baseline.
    /// </summary>
    private static PlanRecoveryDto ProjectPlan(IReadOnlyList<DebtDto> debts, HouseholdCashOutlookInput cash)
    {
        var prepared = HouseholdRecovery.Prepare(
            cash.PlanningCurrency,
            cash.AsOf,
            debts.Select(ToDebt).ToList());
        var comparison = PayoffRollover.Compare(prepared.Rollover);
        var report = CashFlowRecovery.Track(comparison);
        var outlook = HouseholdCashOutlook.Project(cash, prepared.Rollover.Debts, comparison);
        return PlanRecoveryDtoMapper.Map(comparison, report, prepared.MissingBalance, debts.Count > 0, outlook);
    }

    /// <summary>
    /// Copies the balance and limit already in use. A stored balance that following replaced is not the amount owed.
    /// </summary>
    private static HouseholdRecoveryDebt ToDebt(DebtDto debt)
    {
        return new HouseholdRecoveryDebt(
            debt.Id,
            debt.Name,
            debt.Currency,
            debt.Kind,
            debt.BalanceInUse,
            debt.Apr,
            debt.PromotionalApr,
            debt.PromotionalEndsOn,
            debt.MinimumPayment,
            debt.RemainingTermMonths,
            debt.NextDueDate,
            debt.CreditLimitInUse);
    }

    #endregion
}
