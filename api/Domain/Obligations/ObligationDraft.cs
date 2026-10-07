using Cardui.Api.Models;

namespace Cardui.Api.Domain.Obligations;

/// <summary>
/// One valid bill after the name, amount, and date are checked.
/// Amount is one payment, not a monthly equivalent.
/// AccountId is null when the bill is not paid from a household account.
/// </summary>
public readonly record struct ObligationDraft(
    string Name,
    decimal Amount,
    ObligationCadence Cadence,
    DateOnly NextDueDate,
    Guid? AccountId,
    ObligationFlexibility Flexibility);
