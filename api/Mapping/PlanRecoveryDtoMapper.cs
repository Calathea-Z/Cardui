using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Mapping;

public static class PlanRecoveryDtoMapper
{
    /// <summary>
    /// Copies the rollover and keep-all paths into the shape the Plan page renders.
    /// The payoff comparison supplies each debt's outcome, balance points, debt-free date, and interest.
    /// The cash-flow report supplies the steps, obligations, and breathing room.
    /// The partial reclaim path, the written explanations, and the written assumptions are not sent; the page writes its own copy.
    /// HasDebts records whether a debt exists, including one the payoff left out.
    /// The cash outlook follows the same two paths.
    /// </summary>
    public static PlanRecoveryDto Map(
        PayoffRolloverComparison comparison,
        CashFlowRecoveryReport report,
        IReadOnlyList<HouseholdRecoveryMissingBalance> missingBalance,
        bool hasDebts,
        HouseholdCashOutlookReport cashOutlook)
    {
        return new PlanRecoveryDto
        {
            PlanningCurrency = report.PlanningCurrency,
            Rollover = MapPath(comparison.Rollover, report.Rollover),
            ReclaimAll = MapPath(comparison.ReclaimAll, report.ReclaimAll),
            ExcludedCurrencies = report.ExcludedCurrencies,
            MissingBalance = missingBalance
                .Select(debt => new PlanMissingBalanceDto { DebtId = debt.DebtId, Name = debt.Name })
                .ToList(),
            HasDebts = hasDebts,
            CashOutlook = PlanCashOutlookDtoMapper.Map(cashOutlook),
            MonthlyExtra = comparison.MonthlyExtra
        };
    }

    #region Private Methods

    /// <summary>
    /// Copies one path from its payoff projection and its cash-flow recovery.
    /// </summary>
    private static PlanRecoveryPathDto MapPath(PayoffRolloverPath payoff, CashFlowRecoveryPath recovery)
    {
        var lastPoints = LastPoints(payoff.BalancePoints);
        return new PlanRecoveryPathDto
        {
            Kind = recovery.Kind,
            Steps = recovery.Steps.Select(MapStep).ToList(),
            StartingObligation = recovery.StartingObligation,
            RemainingObligation = recovery.RemainingObligation,
            RecurringRoom = recovery.RecurringRoom,
            PaidOffOn = payoff.PaidOffOn,
            TotalInterest = payoff.TotalInterest,
            Debts = payoff.Debts
                .Select(debt => MapDebt(debt, lastPoints.GetValueOrDefault(debt.DebtId)))
                .ToList(),
            BalancePoints = payoff.BalancePoints.Select(MapPoint).ToList()
        };
    }

    /// <summary>
    /// Copies one payoff. A missing removal date stays null.
    /// </summary>
    private static PlanRecoveryStepDto MapStep(CashFlowRecoveryStep step)
    {
        return new PlanRecoveryStepDto
        {
            DebtId = step.DebtId,
            Name = step.Name,
            EndedOn = step.EndedOn,
            StartsOn = step.StartsOn,
            Minimum = step.Minimum,
            BreathingRoom = step.BreathingRoom
        };
    }

    /// <summary>
    /// The last modeled payment for each debt. Points arrive in due-date order, so the last one seen wins.
    /// </summary>
    private static Dictionary<Guid, PayoffBalancePoint> LastPoints(IReadOnlyList<PayoffBalancePoint> points)
    {
        var last = new Dictionary<Guid, PayoffBalancePoint>();
        foreach (var point in points)
        {
            last[point.DebtId] = point;
        }

        return last;
    }

    /// <summary>
    /// Copies one debt's outcome with its last modeled month. An unknown minimum stays null, and so does a month that was never modeled.
    /// </summary>
    private static PlanDebtOutcomeDto MapDebt(PayoffDebtOutcome debt, PayoffBalancePoint? lastPoint)
    {
        return new PlanDebtOutcomeDto
        {
            DebtId = debt.DebtId,
            Name = debt.Name,
            Stop = debt.Stop,
            Balance = debt.Balance,
            Minimum = debt.Minimum,
            PaidOffOn = debt.PaidOffOn,
            LastMonthInterest = lastPoint?.Interest,
            LastMonthPayment = lastPoint?.Payment
        };
    }

    /// <summary>
    /// Copies one balance after a payment.
    /// </summary>
    private static PlanBalancePointDto MapPoint(PayoffBalancePoint point)
    {
        return new PlanBalancePointDto
        {
            DebtId = point.DebtId,
            DueDate = point.DueDate,
            Balance = point.Balance
        };
    }

    #endregion
}
