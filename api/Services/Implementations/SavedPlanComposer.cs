using Cardui.Api.Domain.Debts;
using Cardui.Api.Domain.Plans;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Services.Implementations;

/// <summary>
/// Turns the household's recovery report into the decision plan that can be saved.
/// The selected path is roll payments forward or free up cash. The report is already calculated.
/// </summary>
internal static class SavedPlanComposer
{
    /// <summary>
    /// Builds the decision plan for the path the household selected.
    /// </summary>
    public static DecisionPlan Compose(PlanRecoveryDto report, PayoffRolloverKind kind)
    {
        return DecisionPlanRules.Compose(ToInput(report, kind));
    }

    #region Private Methods

    /// <summary>
    /// Copies the figures the selected path used. A scenario override replaces the saved spending or floor.
    /// Debts stay in payoff order. A debt with no balance is named apart and is not given a zero balance.
    /// </summary>
    private static DecisionPlanInput ToInput(PlanRecoveryDto report, PayoffRolloverKind kind)
    {
        var rollover = kind == PayoffRolloverKind.Rollover;
        var path = rollover ? report.Rollover : report.ReclaimAll;
        var outlook = report.CashOutlook;
        var forecast = (rollover ? outlook.Rollover : outlook.ReclaimAll).Typical;
        var spendable = rollover ? report.Spendable.Rollover : report.Spendable.ReclaimAll;
        var factsById = report.DebtFacts.ToDictionary(debt => debt.DebtId);

        return new DecisionPlanInput(
            outlook.AsOf,
            report.PlanningCurrency,
            kind,
            Facts(report, path, forecast, spendable),
            new DecisionPlanScenario(
                report.MonthlyExtra,
                report.IncomeLossMonths,
                report.WindfallAmount,
                report.WindfallOn,
                report.ScenarioLivingSpendingMonthly,
                report.ScenarioCashFloor),
            path.Debts
                .Select(debt => new DecisionPlanDebt(
                    debt.DebtId,
                    debt.Name,
                    debt.Balance,
                    debt.Minimum,
                    factsById.GetValueOrDefault(debt.DebtId)?.Apr,
                    debt.Stop,
                    debt.PaidOffOn))
                .ToList(),
            path.Steps
                .Select(step => new DecisionPlanStep(
                    step.DebtId,
                    step.Name,
                    step.EndedOn,
                    step.StartsOn,
                    step.Minimum,
                    step.BreathingRoom))
                .ToList(),
            report.MissingBalance.Select(debt => debt.Name).ToList(),
            Excluded(report),
            spendable.Uncertainties
                .Select(item => new DecisionPlanUncertainty(item.Kind, item.Name))
                .ToList());
    }

    /// <summary>
    /// The cash and payoff figures for the selected path, including the extra-payment guardrail.
    /// </summary>
    private static DecisionPlanFacts Facts(
        PlanRecoveryDto report,
        PlanRecoveryPathDto path,
        PlanCashForecastDto forecast,
        SpendableEstimateDto spendable)
    {
        var outlook = report.CashOutlook;
        var guardrail = spendable.Guardrail;
        return new DecisionPlanFacts(
            outlook.StartingCash,
            outlook.StartingReserve,
            outlook.StartingAvailable,
            outlook.StartingCashAccountCount,
            outlook.StartingCashOldestAsOf,
            outlook.StartingCashStaleConnectedCount,
            outlook.HasIncome,
            outlook.HasBills,
            report.ScenarioLivingSpendingMonthly ?? report.LivingSpendingMonthly,
            report.ScenarioCashFloor ?? report.CashFloor,
            report.HasEmergencyGoal,
            report.NamedSavingsGoalCount,
            report.DebtFacts.Count(debt =>
                debt.Freshness is not null && debt.Freshness != DebtLinkFreshness.Current),
            report.DebtFacts.Count(debt => debt.NeedsPaymentReview),
            spendable.Spendable,
            spendable.AlreadyShort,
            new DecisionPlanGuardrail(
                guardrail.Active,
                guardrail.OrdinaryExpenses,
                guardrail.NearTermObligations,
                guardrail.On,
                guardrail.Shortfall,
                guardrail.AlreadyShortWithoutExtra),
            forecast.ShortfallOn,
            forecast.RecoveredOn,
            forecast.ReserveShortfallOn,
            path.StartingObligation,
            path.RecurringRoom,
            path.PaidOffOn,
            path.TotalInterest);
    }

    /// <summary>
    /// Currency codes left out of the payoff or the cash outlook, once each.
    /// </summary>
    private static List<string> Excluded(PlanRecoveryDto report)
    {
        return report.ExcludedCurrencies
            .Concat(report.CashOutlook.ExcludedCurrencies)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();
    }

    #endregion
}
