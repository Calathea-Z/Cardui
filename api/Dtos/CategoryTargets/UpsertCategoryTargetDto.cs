namespace Cardui.Api.Dtos.CategoryTargets;

/// <summary>
/// A target amount and rollover choice for one category in one month.
/// Amount may be zero. Rollover carries that month's remaining into the next month.
/// </summary>
public sealed class UpsertCategoryTargetDto
{
    public int Year { get; set; }

    public int Month { get; set; }

    public decimal Amount { get; set; }

    public bool Rollover { get; set; }
}
