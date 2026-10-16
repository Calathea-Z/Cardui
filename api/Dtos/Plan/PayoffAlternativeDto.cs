using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// One payment-priority alternative.
/// InterestDifference and TradeoffCash are from the priority comparison, before a freed payment rolls.
/// A positive interest difference costs more than avalanche. DebtIds is who extra follows.
/// </summary>
public class PayoffAlternativeDto
{
    public PayoffPriorityChoice Choice { get; set; }

    /// <summary>
    /// True when this reason became the recommendation.
    /// </summary>
    public bool Applied { get; set; }

    /// <summary>
    /// True when this order is not the avalanche order.
    /// </summary>
    public bool ChangesOrder { get; set; }

    public decimal InterestDifference { get; set; }

    public decimal TradeoffCash { get; set; }

    public IReadOnlyList<Guid> DebtIds { get; set; } = [];
}
