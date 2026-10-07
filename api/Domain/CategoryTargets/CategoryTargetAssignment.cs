namespace Cardui.Api.Domain.CategoryTargets;

/// <summary>
/// A saved target for one spending category in one month.
/// Amount is the target. Rollover carries that month's remaining into the next month.
/// </summary>
public sealed record CategoryTargetAssignment(
    Guid CategoryId,
    decimal Amount,
    bool Rollover);
