namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// A debt the recommendation must place first or last.
/// The first constraint for a debt is the one that is used. A later one for the same debt is ignored.
/// A debt that is missing a rate, minimum, or due date stays where avalanche put it.
/// </summary>
public sealed record PayoffConstraint(
    Guid DebtId,
    PayoffConstraintKind Kind);
