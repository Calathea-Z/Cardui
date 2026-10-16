using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Mapping;

public static class PlanRecoveryDtoMapper
{
    /// <summary>
    /// Copies the keep-none, keep-some, and keep-all paths into the shape the Plan page renders.
    /// The payoff comparison supplies each debt's outcome, balance points, debt-free date, and interest.
    /// The cash-flow report supplies the steps, obligations, and breathing room.
    /// Priority alternatives are the payment orders compared with avalanche. Written explanations are not sent.
    /// HasDebts records whether a debt exists, including one the payoff left out.
    /// The cash outlook and spendable estimate follow the same three paths.
    /// </summary>
    public static PlanRecoveryDto Map(
        PayoffRolloverComparison comparison,
        CashFlowRecoveryReport report,
        IReadOnlyList<HouseholdRecoveryMissingBalance> missingBalance,
        bool hasDebts,
        HouseholdCashOutlookReport cashOutlook,
        CashPosition cashPosition,
        SpendableReport spendable,
        PayoffPriorityChoice priority,
        Guid? constraintDebtId,
        PayoffConstraintKind? constraintKind,
        IReadOnlyList<PayoffAlternative> alternatives)
    {
        return new PlanRecoveryDto
        {
            PlanningCurrency = report.PlanningCurrency,
            Rollover = MapPath(comparison.Rollover, report.Rollover),
            Reclaim = MapPath(comparison.Reclaim, report.Reclaim),
            ReclaimAll = MapPath(comparison.ReclaimAll, report.ReclaimAll),
            ExcludedCurrencies = report.ExcludedCurrencies,
            MissingBalance = missingBalance
                .Select(debt => new PlanMissingBalanceDto { DebtId = debt.DebtId, Name = debt.Name })
                .ToList(),
            HasDebts = hasDebts,
            CashOutlook = PlanCashOutlookDtoMapper.Map(cashOutlook, cashPosition),
            Spendable = SpendableEstimateDtoMapper.Map(spendable),
            MonthlyExtra = comparison.MonthlyExtra,
            Priority = priority,
            ConstraintDebtId = constraintDebtId,
            ConstraintKind = constraintKind,
            ReclaimAmount = comparison.ReclaimAmount,
            PriorityAlternatives = alternatives.Select(MapAlternative).ToList()
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
            BalancePoints = payoff.BalancePoints.Select(MapPoint).ToList(),
            CashReclaimed = payoff.CashReclaimed,
            CashRolled = payoff.CashRolled
        };
    }

    /// <summary>
    /// Copies one priority alternative. The page writes the sentence from these fields.
    /// </summary>
    private static PayoffAlternativeDto MapAlternative(PayoffAlternative alternative)
    {
        return new PayoffAlternativeDto
        {
            Choice = alternative.Choice,
            Applied = alternative.Applied,
            ChangesOrder = alternative.ChangesOrder,
            InterestDifference = alternative.InterestDifference,
            TradeoffCash = alternative.TradeoffCash,
            DebtIds = alternative.DebtIds
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
