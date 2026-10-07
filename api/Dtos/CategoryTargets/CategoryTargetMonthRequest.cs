namespace Cardui.Api.Dtos.CategoryTargets;

/// <summary>
/// The year and month a target action applies to.
/// </summary>
public sealed class CategoryTargetMonthRequest
{
    public int Year { get; set; }

    public int Month { get; set; }
}
