using Cardui.Api.Models;

namespace Cardui.Api.Domain;

/// <summary>
/// One valid income source after names, amounts, and dates are checked.
/// TakeHomeAmount is the net amount of a single payment, not a monthly equivalent.
/// </summary>
public readonly record struct IncomeSourceDraft(
    string Name,
    decimal TakeHomeAmount,
    IncomeCadence Cadence,
    DateOnly NextPaymentDate,
    Guid? ContributorId,
    IncomeReliability Reliability);
