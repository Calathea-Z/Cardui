using Cardui.Api.Domain.Plans;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// One assumption, risk, gap, or review condition.
/// Name, amount, date, and count are null when that kind does not use them.
/// </summary>
public class DecisionPlanDetailDto
{
    public DecisionPlanDetailKind Kind { get; set; }

    public string? Name { get; set; }

    public decimal? Amount { get; set; }

    public DateOnly? On { get; set; }

    public int? Count { get; set; }
}
