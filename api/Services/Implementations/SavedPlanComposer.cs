using Cardui.Api.Domain.Debts;
using Cardui.Api.Domain.Plans;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Services.Implementations;

/// <summary>
/// Turns the household's recovery report into the decision plan that can be saved.
/// The selected path keeps none, some, or all of the freed cash. The report is already calculated.
/// </summary>
internal static class SavedPlanComposer
{
    /// <summary>
    /// Builds the decision plan for the path the household selected.
    /// The selected priority and freed-cash choice are the ones stored. A comparison that was not selected is left out.
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
        var path = SelectedPath(report, kind);
        var outlook = report.CashOutlook;
        var forecast = Outlook(outlook, kind).Typical;
        var spendable = Spendable(report.Spendable, kind);
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
                report.ScenarioCashFloor,
                report.Priority,
                report.Priority == PayoffPriorityChoice.Constraint ? report.ConstraintDebtId : null,
                report.Priority == PayoffPriorityChoice.Constraint ? report.ConstraintKind : null,
                kind == PayoffRolloverKind.Reclaim ? report.ReclaimAmount : 0),
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
    /// The payoff path for the freed-cash choice. Keeping some uses the partial path.
    /// </summary>
    private static PlanRecoveryPathDto SelectedPath(PlanRecoveryDto report, PayoffRolloverKind kind)
    {
        return kind switch
        {
            PayoffRolloverKind.Reclaim => report.Reclaim,
            PayoffRolloverKind.ReclaimAll => report.ReclaimAll,
            _ => report.Rollover
        };
    }

    /// <summary>
    /// The cash outlook for the freed-cash choice.
    /// </summary>
    private static PlanCashOutlookPathDto Outlook(PlanCashOutlookDto outlook, PayoffRolloverKind kind)
    {
        return kind switch
        {
            PayoffRolloverKind.Reclaim => outlook.Reclaim,
            PayoffRolloverKind.ReclaimAll => outlook.ReclaimAll,
            _ => outlook.Rollover
        };
    }

    /// <summary>
    /// The spendable estimate for the freed-cash choice.
    /// </summary>
    private static SpendableEstimateDto Spendable(SpendableReportDto spendable, PayoffRolloverKind kind)
    {
        return kind switch
        {
            PayoffRolloverKind.Reclaim => spendable.Reclaim,
            PayoffRolloverKind.ReclaimAll => spendable.ReclaimAll,
            _ => spendable.Rollover
        };
    }

    /// <summary>
    /// The cash and payoff figures for the selected path, including the extra-payment guardrail.
    /// LowestAvailable stays negative when the window is short. Spendable remains floored at zero.
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
            path.TotalInterest,
            spendable.LowestAvailable,
            spendable.LowestOn);
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
