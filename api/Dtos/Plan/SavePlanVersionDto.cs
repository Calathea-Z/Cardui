using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// The scenario to store as a draft, and the draft this request expects to replace.
/// ReplacesDraftId is null only for the first draft.
/// Kind is keep none, keep some, or keep all of the freed cash. Priority is who extra and a rolled payment follow.
/// </summary>
public class SavePlanVersionDto
{
    public PayoffRolloverKind Kind { get; set; }

    public decimal MonthlyExtra { get; set; }

    public int IncomeLossMonths { get; set; }

    public decimal WindfallAmount { get; set; }

    public DateOnly? WindfallOn { get; set; }

    public decimal? LivingSpendingMonthly { get; set; }

    public decimal? CashFloor { get; set; }

    /// <summary>
    /// Who extra and a rolled payment follow. Avalanche is the baseline when this is omitted.
    /// </summary>
    public PayoffPriorityChoice Priority { get; set; }

    /// <summary>
    /// The debt to pay first or last. Null unless <see cref="Priority"/> is the user constraint.
    /// </summary>
    public Guid? ConstraintDebtId { get; set; }

    /// <summary>
    /// Pay first or pay last for <see cref="ConstraintDebtId"/>.
    /// </summary>
    public PayoffConstraintKind? ConstraintKind { get; set; }

    /// <summary>
    /// How much freed cash to keep each month when <see cref="Kind"/> keeps some. Zero keeps none.
    /// </summary>
    public decimal ReclaimAmount { get; set; }

    public Guid? ReplacesDraftId { get; set; }
}
