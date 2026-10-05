using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Debts;

public class UpsertDebtDto
{
    public string? Name { get; set; }

    public DebtKind? Kind { get; set; }

    public Guid? AccountId { get; set; }

    public decimal? Balance { get; set; }

    public DateOnly? BalanceAsOf { get; set; }

    public decimal? Apr { get; set; }

    public decimal? MinimumPayment { get; set; }

    public DateOnly? NextDueDate { get; set; }

    public decimal? CreditLimit { get; set; }

    public int? RemainingTermMonths { get; set; }

    public decimal? PromotionalApr { get; set; }

    public DateOnly? PromotionalEndsOn { get; set; }
}
