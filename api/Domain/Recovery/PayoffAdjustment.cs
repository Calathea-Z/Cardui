namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One compared reason, whether or not it became the recommendation.
/// Applied means this reason is the recommended order. InterestDifference is this order's interest minus avalanche.
/// A positive difference costs more than avalanche.
/// TradeoffCash is the dollar figure set beside that interest. For a minimum, it is each minimum times the months it ends sooner, net of minimums that end later.
/// For utilization, it is each high-utilization minimum times the months sooner the balance falls under 90 percent.
/// For a constraint, it is the same net minimum cash, and the constraint is honored even when interest is higher.
/// DebtIds is the order this reason would use. Explanation states whether it changed the recommendation.
/// </summary>
public sealed record PayoffAdjustment(
    PayoffAdjustmentKind Kind,
    bool Applied,
    decimal InterestDifference,
    decimal TradeoffCash,
    IReadOnlyList<Guid> DebtIds,
    string Explanation);
