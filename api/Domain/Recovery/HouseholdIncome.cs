using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One income source as the cash outlook reads it.
/// TypicalAmount and LowAmount are one payment. LowAmount is null when no low payment is recorded.
/// Raises are later typical amounts. ContributorId is null when the paycheck is not assigned to a person.
/// Reliability says whether that paycheck can be counted as money that will arrive. Steady can. Variable and Uncertain cannot.
/// </summary>
public sealed record HouseholdIncome(
    Guid Id,
    string Name,
    string Currency,
    decimal TypicalAmount,
    decimal? LowAmount,
    IncomeCadence Cadence,
    DateOnly NextPaymentDate,
    IReadOnlyList<DatedIncomeRaise> Raises,
    Guid? ContributorId,
    IncomeReliability Reliability);
