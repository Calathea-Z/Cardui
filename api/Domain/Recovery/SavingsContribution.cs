namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Cash set aside on one date.
/// Amount is a reservation. It is not recorded as spending.
/// </summary>
public sealed record SavingsContribution(
    DateOnly Date,
    decimal Amount);
