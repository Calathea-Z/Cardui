namespace Cardui.Api.Domain.Obligations;

/// <summary>
/// One day of charges for a merchant, reduced to the payment the finder uses.
/// Amount is the largest charge that day.
/// </summary>
internal sealed record RecurringSuggestionEvent(
    DateOnly Date,
    decimal Amount,
    Guid AccountId,
    string? AccountName,
    string DisplayName);
