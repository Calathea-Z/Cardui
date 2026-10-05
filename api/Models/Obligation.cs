namespace Cardui.Api.Models;

public class Obligation
{
    public const int NameMaxLength = 80;

    public Guid Id { get; init; }

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public required string Name { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public ObligationCadence Cadence { get; set; }

    public DateOnly NextDueDate { get; set; }

    public Guid? AccountId { get; set; }

    public Account? Account { get; set; }

    public ObligationFlexibility Flexibility { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}
