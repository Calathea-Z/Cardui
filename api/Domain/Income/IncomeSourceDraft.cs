using Cardui.Api.Models;

namespace Cardui.Api.Domain.Income;

/// <summary>
/// One valid income source after names, amounts, and dates are checked.
/// TakeHomeAmount is the typical net amount of a single payment, not a monthly equivalent.
/// Low and strong are optional other payments. Gross pay is optional pay for that same payment before deductions.
/// Raises are later typical amounts and do not replace the current one.
/// </summary>
public readonly record struct IncomeSourceDraft(
    string Name,
    decimal TakeHomeAmount,
    decimal? LowTakeHomeAmount,
    decimal? StrongTakeHomeAmount,
    decimal? GrossPayAmount,
    IncomeCadence Cadence,
    DateOnly NextPaymentDate,
    Guid? ContributorId,
    IncomeReliability Reliability,
    IReadOnlyList<IncomeRaiseDraft> Raises);
