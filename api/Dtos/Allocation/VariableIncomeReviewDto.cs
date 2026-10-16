namespace Cardui.Api.Dtos.Allocation;

/// <summary>
/// One variable source prepared for review.
/// Surplus is null when the source is left out. LeftOut is null when Surplus is set.
/// </summary>
public sealed class VariableIncomeReviewDto
{
    public string Name { get; set; } = "";

    public string Currency { get; set; } = "";

    public decimal? Surplus { get; set; }

    public string? LeftOut { get; set; }
}
