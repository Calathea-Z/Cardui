namespace Cardui.Api.Domain;

/// <summary>
/// One category's target, rolled amount, spent, and remaining for a month.
/// A null target means no target is set. Available and remaining are null in that case.
/// RolloverIn is the previous month's remaining when that month turned rollover on, and it can be negative.
/// </summary>
public sealed record CategoryTargetProgress(
    Guid? CategoryId,
    decimal? Target,
    bool Rollover,
    decimal RolloverIn,
    decimal Spent,
    decimal? Available,
    decimal? Remaining);
