namespace Cardui.Api.Dtos.Allocation;

/// <summary>
/// The household's allocation preference and the variable sources it can review.
/// IsSaved is false when no row is stored. The percents are then zero, so the whole amount stays available.
/// </summary>
public sealed class AllocationPageDto
{
    public string PlanningCurrency { get; set; } = "";

    public bool IsSaved { get; set; }

    public AllocationRuleDto VariableIncome { get; set; } = new();

    public AllocationRuleDto Windfall { get; set; } = new();

    public List<VariableIncomeReviewDto> VariableIncomeReviews { get; set; } = [];
}
