using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

public class PlanRecoveryPathDto
{
    public PayoffRolloverKind Kind { get; set; }

    public IReadOnlyList<PlanRecoveryStepDto> Steps { get; set; } = [];

    /// <summary>
    /// Known minimums before any payoff. Null when every minimum is unknown.
    /// </summary>
    public decimal? StartingObligation { get; set; }

    /// <summary>
    /// Known minimums of debts that are not paid off. Null when every remaining minimum is unknown.
    /// </summary>
    public decimal? RemainingObligation { get; set; }

    /// <summary>
    /// The monthly amount available after the last recorded change.
    /// </summary>
    public decimal RecurringRoom { get; set; }

    /// <summary>
    /// The due date that clears the last debt. Null while any debt in the plan is still open or cannot be calculated.
    /// </summary>
    public DateOnly? PaidOffOn { get; set; }

    public decimal TotalInterest { get; set; }

    /// <summary>
    /// Each debt in the plan, in the order rolled cash follows.
    /// </summary>
    public IReadOnlyList<PlanDebtOutcomeDto> Debts { get; set; } = [];

    /// <summary>
    /// Each debt's balance after every modeled payment, by due date and then in the order of Debts.
    /// </summary>
    public IReadOnlyList<PlanBalancePointDto> BalancePoints { get; set; } = [];
}
