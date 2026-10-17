using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One number copied from the plan calculation.
/// Amount, On, and Count are set only for the part that figure has. A blank stays null, and null is not zero.
/// HorizonMonths is null when the figure is not a cash window. Zero is the 30-day window. Six, 12, and 18 are those horizons.
/// Basis is LowPay only for a cash figure that used low pay. Name is the debt, when the figure is about one debt.
/// </summary>
public sealed record PlanningForecastFigure(
    PlanningForecastFigureKind Kind,
    PayoffRolloverKind? Path,
    PayoffPriorityChoice? Priority,
    PlanningForecastBasis Basis,
    decimal? Amount,
    DateOnly? On,
    int? Count,
    int? HorizonMonths,
    string? Name);
