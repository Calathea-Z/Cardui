using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One income source as the cash outlook reads it.
/// TypicalAmount and LowAmount are one payment. LowAmount is null when no low payment is recorded.
/// Raises are later typical amounts.
/// </summary>
public sealed record HouseholdIncome(
    Guid Id,
    string Name,
    string Currency,
    decimal TypicalAmount,
    decimal? LowAmount,
    IncomeCadence Cadence,
    DateOnly NextPaymentDate,
    IReadOnlyList<DatedIncomeRaise> Raises);
