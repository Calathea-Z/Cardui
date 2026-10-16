using Cardui.Api.Domain;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Exceptions;
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
    private readonly ISavingsGoalsService _savingsGoalsService;
    private readonly ILivingService _livingService;
    private readonly TimeProvider _timeProvider;
    private readonly HouseholdScope _householdScope;

    public PlanService(
        IDebtsService debtsService,
        IIncomeSourcesService incomeSourcesService,
        IObligationsService obligationsService,
        IAccountsService accountsService,
        ISavingsGoalsService savingsGoalsService,
        ILivingService livingService,
        TimeProvider timeProvider,
        HouseholdScope householdScope)
    {
        _debtsService = debtsService;
        _incomeSourcesService = incomeSourcesService;
        _obligationsService = obligationsService;
        _accountsService = accountsService;
        _savingsGoalsService = savingsGoalsService;
        _livingService = livingService;
        _timeProvider = timeProvider;
        _householdScope = householdScope;
    }

    /// <inheritdoc />
    public async Task<PlanRecoveryDto> GetRecoveryAsync(
        PlanScenarioQuery scenario,
        CancellationToken cancellationToken = default)
    {
        var today = FinancialDate.Today(_timeProvider, _householdScope.TimeZoneId);
        var debts = await _debtsService.GetDebtsAsync(cancellationToken);
        var loaded = await LoadCashFactsAsync(today, cancellationToken);
        var preview = RequireScenario(scenario, loaded.LivingSpendingMonthly, today);
        return ProjectPlan(debts, loaded, preview);
    }

    #region Private Methods

    /// <summary>
    /// Rejects a preview the forecast cannot run, then rounds the amounts it will use.
    /// A nullable spending or floor amount stays null so the saved figure is kept.
    /// </summary>
    private static PlanCashScenario RequireScenario(
        PlanScenarioQuery scenario,
        decimal storedLivingSpending,
        DateOnly today)
    {
        var message = PlanCashScenarioRules.Reject(
            scenario.MonthlyExtra,
            scenario.IncomeLossMonths,
            scenario.WindfallAmount,
            scenario.WindfallOn,
            scenario.LivingSpendingMonthly,
            scenario.CashFloor,
            storedLivingSpending,
            today);
        if (message is not null)
        {
            throw new BadRequestException(message);
        }

        return new PlanCashScenario(
            AccountLedger.Round(scenario.MonthlyExtra),
            scenario.IncomeLossMonths,
            AccountLedger.Round(scenario.WindfallAmount),
            scenario.WindfallAmount > 0 ? scenario.WindfallOn : null,
            scenario.LivingSpendingMonthly is decimal spending
                ? AccountLedger.Round(spending)
                : null,
            scenario.CashFloor is decimal floor ? AccountLedger.Round(floor) : null);
    }

    /// <summary>
    /// Loads the starting cash, income, bills, and savings the cash outlook reads, for a forecast that starts today.
    /// The loads run one after another because they share one database context.
    /// </summary>
    private async Task<PlanCashFacts> LoadCashFactsAsync(
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var cashPosition = await _accountsService.GetCashPositionAsync(cancellationToken);
        var incomes = await _incomeSourcesService.GetOutlookIncomesAsync(cancellationToken);
        var bills = await _obligationsService.GetOutlookBillsAsync(cancellationToken);
        var savings = await _savingsGoalsService.GetOutlookAsync(today, cancellationToken);
        var sharedIncomes = await _livingService.GetSharedIncomesAsync(incomes, cancellationToken);
        var input = new HouseholdCashOutlookInput(
            _householdScope.PlanningCurrency,
            today,
            cashPosition.Total,
            sharedIncomes,
            bills,
            savings.StartingReserve,
            savings.Contributions);
        return new PlanCashFacts(
            input,
            cashPosition,
            AccountLedger.Round(savings.LivingSpendingMonthly),
            savings.HasCashFloor,
            savings.HasEmergencyGoal,
            savings.NamedGoalCount,
            savings.CashFloor);
    }

    /// <summary>
    /// Runs the approved rollover and recovery rules from the outlook's start date, then the cash outlook and spendable estimate on both paths,
    /// and shapes the result for the page. The preview is tried for this response. Reclaim stays at the baseline.
    /// Spendable counts steady pay only. The extra is compared with the same path at minimums.
    /// </summary>
    private static PlanRecoveryDto ProjectPlan(
        IReadOnlyList<DebtDto> debts,
        PlanCashFacts facts,
        PlanCashScenario scenario)
    {
        var recoveryDebts = debts.Select(ToDebt).ToList();
        var prepared = HouseholdRecovery.Prepare(
            facts.Input.PlanningCurrency,
            facts.Input.AsOf,
            recoveryDebts,
            scenario.MonthlyExtra);
        var comparison = PayoffRollover.Compare(prepared.Rollover);
        var minimums = scenario.MonthlyExtra > 0
            ? PayoffRollover.Compare(HouseholdRecovery.Prepare(
                facts.Input.PlanningCurrency,
                facts.Input.AsOf,
                recoveryDebts,
                0m).Rollover)
            : comparison;
        var report = CashFlowRecovery.Track(comparison);
        var cash = PlanCashScenarioAdjustment.Apply(
            facts.Input,
            facts.LivingSpendingMonthly,
            facts.CashFloor,
            scenario);
        var outlook = HouseholdCashOutlook.Project(cash, prepared.Rollover.Debts, comparison);
        var spendable = SpendableRules.Project(
            cash,
            prepared.Rollover.Debts.Select(debt => debt.Terms).ToList(),
            comparison,
            minimums,
            prepared.MissingBalance.Select(debt => debt.Name).ToList(),
            facts.LivingSpendingMonthly > 0 || scenario.LivingSpendingMonthly is not null);
        var plan = PlanRecoveryDtoMapper.Map(
            comparison,
            report,
            prepared.MissingBalance,
            debts.Count > 0,
            outlook,
            facts.CashPosition,
            spendable);
        plan.LivingSpendingMonthly = facts.LivingSpendingMonthly;
        plan.HasCashFloor = facts.HasCashFloor;
        plan.HasEmergencyGoal = facts.HasEmergencyGoal;
        plan.NamedSavingsGoalCount = facts.NamedSavingsGoalCount;
        plan.DebtFacts = debts.Select(ToDebtFact).ToList();
        plan.IncomeLossMonths = scenario.IncomeLossMonths;
        plan.WindfallAmount = scenario.WindfallAmount;
        plan.WindfallOn = scenario.WindfallOn;
        plan.ScenarioLivingSpendingMonthly = scenario.LivingSpendingMonthly;
        plan.CashFloor = facts.CashFloor;
        plan.ScenarioCashFloor = scenario.CashFloor;
        return plan;
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

    /// <summary>
    /// Copies the balance provenance Plan needs to explain its debt claims.
    /// A saved minimum on a zero balance is retained and marked for review.
    /// </summary>
    private static PlanDebtFactDto ToDebtFact(DebtDto debt)
    {
        return new PlanDebtFactDto
        {
            DebtId = debt.Id,
            Name = debt.Name,
            Balance = debt.BalanceInUse,
            BalanceAsOf = debt.BalanceInUseAsOf,
            BalanceSource = debt.BalanceSource,
            Freshness = debt.Freshness,
            MinimumPayment = debt.MinimumPayment,
            NeedsPaymentReview = debt.BalanceInUse == 0 && debt.MinimumPayment > 0,
            Kind = debt.Kind,
            Apr = debt.Apr,
            Currency = debt.Currency
        };
    }

    #endregion
}
