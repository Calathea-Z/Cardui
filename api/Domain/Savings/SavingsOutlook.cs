using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Savings;

/// <summary>
/// Protected cash for one forecast.
/// StartingReserve counts only goals in the planning currency. Contributions include every goal so another currency can be named and left out.
/// LivingSpendingMonthly is the flexible-spending amount in the planning currency. Zero means it is unset or in another currency.
/// The setup fields distinguish an unset cash foundation from a saved goal whose current amount is zero.
/// </summary>
public sealed record SavingsOutlook(
    decimal StartingReserve,
    IReadOnlyList<CashFlowEvent> Contributions,
    decimal LivingSpendingMonthly,
    bool HasCashFloor,
    bool HasEmergencyGoal,
    int NamedGoalCount);
