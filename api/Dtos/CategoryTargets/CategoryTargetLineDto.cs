namespace Cardui.Api.Dtos.CategoryTargets;

/// <summary>
/// One spending category for a month.
/// A null target means no target is set, and available and remaining are null in that case.
/// RolloverIn is money or overspend from the previous month. It can be negative.
/// Spent is posted spending. It is never negative.
/// </summary>
public sealed class CategoryTargetLineDto
{
    public Guid? CategoryId { get; set; }

    public required string Name { get; set; }

    public string? Color { get; set; }

    public string? Icon { get; set; }

    public required string SubGroupName { get; set; }

    public decimal? Target { get; set; }

    public bool Rollover { get; set; }

    public decimal RolloverIn { get; set; }

    public decimal Spent { get; set; }

    public decimal? Available { get; set; }

    public decimal? Remaining { get; set; }

    public bool CanSetTarget { get; set; }
}
