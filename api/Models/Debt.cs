namespace Cardui.Api.Models;

public class Debt
{
    public const int NameMaxLength = 80;

    public Guid Id { get; init; }

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public required string Name { get; set; }

    public DebtKind Kind { get; set; }

    public Guid? AccountId { get; set; }

    public Account? Account { get; set; }

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

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}
