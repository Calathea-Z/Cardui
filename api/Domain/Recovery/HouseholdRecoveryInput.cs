namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The rollover input built from the household's debts, and the debts it left out for a missing balance.
/// MissingBalance keeps the order the debts were given in.
/// </summary>
public sealed record HouseholdRecoveryInput(
    PayoffRolloverInput Rollover,
    IReadOnlyList<HouseholdRecoveryMissingBalance> MissingBalance);
