namespace Cardui.Api.Models;

public class HouseholdContributor
{
    public const int NameMaxLength = 80;

    public Guid Id { get; init; }

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public required string Name { get; set; }

    public bool IsVisible { get; set; } = true;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}
