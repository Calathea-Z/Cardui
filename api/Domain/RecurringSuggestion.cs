using Cardui.Api.Models;

namespace Cardui.Api.Domain;

/// <summary>
/// A payment pattern noticed in activity. It is not a bill.
/// Amount is one typical payment, not a monthly total.
/// AccountId is null when the charges did not all leave the same account.
/// NextDueDate is the date after the latest charge. It can be today or earlier when that payment is due and the pattern is still recent.
/// </summary>
public sealed record RecurringSuggestion(
    string Key,
    string Name,
    decimal Amount,
    ObligationCadence Cadence,
    DateOnly NextDueDate,
    Guid? AccountId,
    string? AccountName);
