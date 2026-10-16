using Cardui.Api.Models;

namespace Cardui.Api.Domain.Allocation;

/// <summary>
/// One income source as the allocation rule reads it.
/// TypicalAmount and StrongAmount are one payment. StrongAmount is null when no strong payment is recorded.
/// </summary>
public sealed record VariableIncomePayment(
    string Name,
    string Currency,
    IncomeReliability Reliability,
    decimal TypicalAmount,
    decimal? StrongAmount);
