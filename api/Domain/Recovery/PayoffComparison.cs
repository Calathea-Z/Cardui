namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Avalanche, the recommended order, and the user-selected order for one set of debts.
/// Adjustments always contains minimum release, utilization, and constraint, in that order.
/// UserOrderProvided is false when no user order was sent, and UserSelected then matches avalanche.
/// ExcludedCurrencies are codes left out. Assumptions state the ranking and interest rules.
/// The same inputs produce the same orders and the same cents. Nothing is saved.
/// </summary>
public sealed record PayoffComparison(
    string PlanningCurrency,
    decimal MonthlyExtra,
    bool UserOrderProvided,
    PayoffOrder Avalanche,
    PayoffOrder Recommended,
    PayoffOrder UserSelected,
    IReadOnlyList<PayoffAdjustment> Adjustments,
    IReadOnlyList<string> ExcludedCurrencies,
    IReadOnlyList<string> Assumptions);
