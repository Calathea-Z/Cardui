using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Income to place on its own dates.
/// Amount is the payment to use, already chosen as low, typical, or strong. It is not a monthly figure.
/// Raises replace that payment on and after each effective date. Irregular contributes its next date only.
/// </summary>
public sealed record DatedIncome(
    Guid Id,
    string Name,
    string Currency,
    decimal Amount,
    IncomeCadence Cadence,
    DateOnly NextPaymentDate,
    IReadOnlyList<DatedIncomeRaise> Raises);
