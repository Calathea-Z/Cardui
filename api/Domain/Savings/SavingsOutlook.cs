using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Savings;

/// <summary>
/// Protected cash for one forecast.
/// StartingReserve counts only goals in the planning currency. Contributions include every goal so another currency can be named and left out.
/// </summary>
public sealed record SavingsOutlook(
    decimal StartingReserve,
    IReadOnlyList<CashFlowEvent> Contributions);
