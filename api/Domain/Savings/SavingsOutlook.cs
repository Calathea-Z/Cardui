using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Savings;

/// <summary>
/// Protected cash for one forecast.
/// StartingReserve counts only goals in the planning currency. Contributions include every goal so another currency can be named and left out.
/// LivingSpendingMonthly is the flexible-spending amount in the planning currency. Zero means it is unset or in another currency.
/// </summary>
public sealed record SavingsOutlook(
    decimal StartingReserve,
    IReadOnlyList<CashFlowEvent> Contributions,
    decimal LivingSpendingMonthly);
