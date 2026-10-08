using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

public class CashFlowRecoveryPathDto
{
    public PayoffRolloverKind Kind { get; set; }

    public IReadOnlyList<CashFlowRecoveryStepDto> Steps { get; set; } = [];

    /// <summary>
    /// Known minimums before any payoff. Null when every minimum is unknown.
    /// </summary>
    public decimal? StartingObligation { get; set; }

    /// <summary>
    /// Known minimums of debts that are not paid off. Null when every remaining minimum is unknown.
    /// </summary>
    public decimal? RemainingObligation { get; set; }

    public int UnknownRemaining { get; set; }

    public decimal BreathingRoom { get; set; }

    public decimal ReleasedExtra { get; set; }

    public decimal RecurringRoom { get; set; }

    public required string Explanation { get; set; }
}
