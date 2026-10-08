using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Plan;

/// <summary>
/// The source and freshness of one debt balance used by Plan.
/// A zero balance with a stored positive minimum needs review and stays saved on the debt.
/// </summary>
public sealed class PlanDebtFactDto
{
    public Guid DebtId { get; set; }

    public required string Name { get; set; }

    public decimal? Balance { get; set; }

    public DateOnly? BalanceAsOf { get; set; }

    public DebtFieldSource BalanceSource { get; set; }

    public DebtLinkFreshness? Freshness { get; set; }

    public decimal? MinimumPayment { get; set; }

    public bool NeedsPaymentReview { get; set; }
}
