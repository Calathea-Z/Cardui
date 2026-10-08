namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One promotion or constraint order scored against avalanche.
/// InterestDifference and TradeoffCash use the same meaning as on a payoff adjustment.
/// </summary>
internal sealed record PayoffCandidate(
    PayoffAdjustmentKind Kind,
    PayoffOrder Order,
    decimal InterestDifference,
    decimal TradeoffCash);
