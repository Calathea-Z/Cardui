namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// A constraint that was applied or skipped while building an order.
/// Honored is false when the debt is missing a term or is not in the comparison.
/// Name is the debt name when it is known, and a short stand-in when the debt is absent.
/// </summary>
internal sealed record PayoffConstraintNote(
    string Name,
    PayoffConstraintKind Kind,
    bool Honored);
