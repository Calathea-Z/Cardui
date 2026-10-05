namespace Cardui.Api.Models;

public class IncomeSource
{
    public const int NameMaxLength = 80;

    public Guid Id { get; init; }

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public required string Name { get; set; }

    public decimal TakeHomeAmount { get; set; }

    public required string Currency { get; set; }

    public IncomeCadence Cadence { get; set; }

    public DateOnly NextPaymentDate { get; set; }

    public Guid? ContributorId { get; set; }

    public HouseholdContributor? Contributor { get; set; }

    public IncomeReliability Reliability { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}
