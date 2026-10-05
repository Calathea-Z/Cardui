using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Debts;

public class DebtDto
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DebtKind Kind { get; set; }

    public Guid? AccountId { get; set; }

    public string? AccountName { get; set; }

    public decimal? Balance { get; set; }

    public DateOnly? BalanceAsOf { get; set; }

    public required string Currency { get; set; }

    public decimal? Apr { get; set; }

    public decimal? MinimumPayment { get; set; }

    public DateOnly? NextDueDate { get; set; }

    public decimal? CreditLimit { get; set; }

    public int? RemainingTermMonths { get; set; }

    public decimal? PromotionalApr { get; set; }

    public DateOnly? PromotionalEndsOn { get; set; }

    /// <summary>
    /// Share of the credit limit in use, as a ratio. 0.85 means 85 percent.
    /// Null when the balance or the credit limit is unknown. This is calculated and is not stored.
    /// </summary>
    public decimal? Utilization { get; set; }
}
