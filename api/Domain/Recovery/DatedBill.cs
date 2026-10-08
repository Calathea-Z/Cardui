using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// A bill to place on its due dates.
/// Amount is one payment. Irregular contributes its next due date only.
/// </summary>
public sealed record DatedBill(
    Guid Id,
    string Name,
    string Currency,
    decimal Amount,
    ObligationCadence Cadence,
    DateOnly NextDueDate);
