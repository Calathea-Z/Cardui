using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Mapping;

public static class SpendableEstimateDtoMapper
{
    /// <summary>
    /// Copies the three spendable paths into the shape the Plan page renders.
    /// </summary>
    public static SpendableReportDto Map(SpendableReport report)
    {
        return new SpendableReportDto
        {
            Rollover = MapEstimate(report.Rollover),
            Reclaim = MapEstimate(report.Reclaim),
            ReclaimAll = MapEstimate(report.ReclaimAll)
        };
    }

    #region Private Methods

    /// <summary>
    /// Copies one path, including the guardrail and the reasons something was left out.
    /// </summary>
    private static SpendableEstimateDto MapEstimate(SpendableEstimate estimate)
    {
        return new SpendableEstimateDto
        {
            From = estimate.From,
            Through = estimate.Through,
            Spendable = estimate.Spendable,
            LowestAvailable = estimate.LowestAvailable,
            LowestOn = estimate.LowestOn,
            ReliableIncome = estimate.ReliableIncome,
            Obligations = estimate.Obligations,
            OrdinarySpending = estimate.OrdinarySpending,
            Reserved = estimate.Reserved,
            AlreadyShort = estimate.AlreadyShort,
            Guardrail = new SpendableGuardrailDto
            {
                Active = estimate.Guardrail.Active,
                OrdinaryExpenses = estimate.Guardrail.OrdinaryExpenses,
                NearTermObligations = estimate.Guardrail.NearTermObligations,
                On = estimate.Guardrail.On,
                Shortfall = estimate.Guardrail.Shortfall,
                AlreadyShortWithoutExtra = estimate.Guardrail.AlreadyShortWithoutExtra
            },
            Uncertainties = estimate.Uncertainties
                .Select(item => new SpendableUncertaintyDto { Kind = item.Kind, Name = item.Name })
                .ToList()
        };
    }

    #endregion
}
