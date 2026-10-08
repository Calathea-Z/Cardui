namespace Cardui.Api.Models;

public class HouseholdContributor
{
    public const int NameMaxLength = 80;

    public Guid Id { get; init; }

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public required string Name { get; set; }

    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// The current monthly benchmark used to derive this person's share of scheduled pay.
    /// Null shares all recorded pay. Zero shares none. Low pay and future raises keep the derived share.
    /// </summary>
    public decimal? MonthlyContribution { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; set; }
}
