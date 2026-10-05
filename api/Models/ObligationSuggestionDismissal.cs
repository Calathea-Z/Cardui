namespace Cardui.Api.Models;

/// <summary>
/// A recurring pattern the household has left out of bills.
/// The key is the normalized merchant text. The same pattern is not suggested again.
/// </summary>
public class ObligationSuggestionDismissal
{
    public const int KeyMaxLength = 200;

    public Guid Id { get; init; }

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public required string Key { get; set; }

    public DateTimeOffset DismissedAt { get; init; }
}
