using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Mapping;

public static class CashFlowRecoveryDtoMapper
{
    /// <summary>
    /// Copies a cash-flow recovery report into the API shape.
    /// HasDebts records whether a debt exists, including one the payoff left out.
    /// </summary>
    public static CashFlowRecoveryReportDto Map(CashFlowRecoveryReport report, bool hasDebts)
    {
        return new CashFlowRecoveryReportDto
        {
            PlanningCurrency = report.PlanningCurrency,
            MonthlyExtra = report.MonthlyExtra,
            ReclaimAmount = report.ReclaimAmount,
            Rollover = MapPath(report.Rollover),
            Reclaim = MapPath(report.Reclaim),
            ReclaimAll = MapPath(report.ReclaimAll),
            ExcludedCurrencies = report.ExcludedCurrencies,
            Assumptions = report.Assumptions,
            HasDebts = hasDebts
        };
    }

    #region Private Methods

    /// <summary>
    /// Copies one path, including the explanation of debts the projection could not finish.
    /// </summary>
    private static CashFlowRecoveryPathDto MapPath(CashFlowRecoveryPath path)
    {
        return new CashFlowRecoveryPathDto
        {
            Kind = path.Kind,
            Steps = path.Steps.Select(MapStep).ToList(),
            StartingObligation = path.StartingObligation,
            RemainingObligation = path.RemainingObligation,
            UnknownRemaining = path.UnknownRemaining,
            BreathingRoom = path.BreathingRoom,
            ReleasedExtra = path.ReleasedExtra,
            RecurringRoom = path.RecurringRoom,
            Explanation = path.Explanation
        };
    }

    /// <summary>
    /// Copies one payoff. A missing removal date stays null.
    /// </summary>
    private static CashFlowRecoveryStepDto MapStep(CashFlowRecoveryStep step)
    {
        return new CashFlowRecoveryStepDto
        {
            DebtId = step.DebtId,
            Name = step.Name,
            EndedOn = step.EndedOn,
            StartsOn = step.StartsOn,
            Minimum = step.Minimum,
            Extra = step.Extra,
            Amount = step.Amount,
            BreathingRoomAdded = step.BreathingRoomAdded,
            BreathingRoom = step.BreathingRoom
        };
    }

    #endregion
}
