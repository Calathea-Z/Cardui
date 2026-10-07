namespace Cardui.Api.Dtos.CategoryTargets;

/// <summary>
/// Monthly category targets and how much of each has been spent.
/// TargetTotal and Remaining are null when no category has a target.
/// Spent includes spending with no target. OtherSpent is that part, and it is not subtracted from Remaining.
/// UnassignedRolloverCount is categories that still have last month's leftover or overspend and no target yet.
/// A null CopiedFrom year means this month was not copied from an earlier month.
/// </summary>
public sealed class CategoryTargetMonthDto
{
    public int Year { get; set; }

    public int Month { get; set; }

    public bool Saved { get; set; }

    public int? CopiedFromYear { get; set; }

    public int? CopiedFromMonth { get; set; }

    public required string PlanningCurrency { get; set; }

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public bool ThroughToday { get; set; }

    public int TodayYear { get; set; }

    public int TodayMonth { get; set; }

    public decimal? TargetTotal { get; set; }

    public int MissingTargetCount { get; set; }

    public int UnassignedRolloverCount { get; set; }

    public decimal Spent { get; set; }

    public decimal OtherSpent { get; set; }

    public decimal? Remaining { get; set; }

    public int ExcludedTransactionCount { get; set; }

    public IReadOnlyList<string> ExcludedCurrencies { get; set; } = [];

    public IReadOnlyList<CategoryTargetLineDto> Categories { get; set; } = [];
}
