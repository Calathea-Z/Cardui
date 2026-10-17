using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Planning;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanningForecasts
{
    /// <summary>
    /// Returns the forecast for one temporary scenario.
    /// Every amount and date is copied from the plan calculation. Assumptions and tradeoffs stay with those numbers.
    /// Nothing is saved, and the planning model is not called.
    /// </summary>
    Task<PlanningForecastDto> DescribeAsync(
        PlanScenarioQuery scenario,
        CancellationToken cancellationToken = default);
}
