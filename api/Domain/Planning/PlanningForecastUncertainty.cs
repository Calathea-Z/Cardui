using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One reason spendable cash leaves something out.
/// Name is set when the calculation named an income source, a debt, or a currency. A blank name stays null.
/// </summary>
public sealed record PlanningForecastUncertainty(
    PayoffRolloverKind Path,
    SpendableUncertaintyKind Kind,
    string? Name);
