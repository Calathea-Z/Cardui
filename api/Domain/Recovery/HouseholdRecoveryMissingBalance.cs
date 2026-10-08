namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// A household debt left out of the plan because its balance is unknown.
/// The page names it so the person can add the balance.
/// </summary>
public sealed record HouseholdRecoveryMissingBalance(
    Guid DebtId,
    string Name);
