using Cardui.Api.Domain.Planning;
using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// One number copied from the plan calculation.
/// Amount, On, and Count are null when that figure does not have that part. Null is not zero.
/// HorizonMonths is null outside a cash window. Zero is the 30-day window.
/// </summary>
public sealed class PlanningForecastFigureDto
{
    public PlanningForecastFigureKind Kind { get; init; }

    public PayoffRolloverKind? Path { get; init; }

    public PayoffPriorityChoice? Priority { get; init; }

    public PlanningForecastBasis Basis { get; init; }

    public decimal? Amount { get; init; }

    public DateOnly? On { get; init; }

    public int? Count { get; init; }

    public int? HorizonMonths { get; init; }

    public string? Name { get; init; }
}
