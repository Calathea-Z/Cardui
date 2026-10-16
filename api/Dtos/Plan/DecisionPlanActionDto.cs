using Cardui.Api.Domain.Plans;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// One ranked action in a decision plan.
/// Order starts at 1. For LowerExtra, Count is 1 for ordinary spending, 2 for a near-term bill or debt payment, and 3 for both.
/// AlreadyShortWithoutExtra is true only for that extra-payment action.
/// </summary>
public class DecisionPlanActionDto
{
    public int Order { get; set; }

    public DecisionPlanActionKind Kind { get; set; }

    public string? Name { get; set; }

    public string? NextName { get; set; }

    public decimal? Amount { get; set; }

    public decimal? Extra { get; set; }

    public DateOnly? On { get; set; }

    public int? Count { get; set; }

    public bool AlreadyShortWithoutExtra { get; set; }
}
