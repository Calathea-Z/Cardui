namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One payment-priority alternative compared with pure avalanche.
/// InterestDifference and TradeoffCash come from the priority comparison, which does not roll a freed payment.
/// A positive interest difference costs more than avalanche. Applied means this reason became the recommendation.
/// ChangesOrder is false when the debt ids match avalanche. DebtIds is who extra follows, not the payoff dates.
/// </summary>
public sealed record PayoffAlternative(
    PayoffPriorityChoice Choice,
    bool Applied,
    bool ChangesOrder,
    decimal InterestDifference,
    decimal TradeoffCash,
    IReadOnlyList<Guid> DebtIds);
