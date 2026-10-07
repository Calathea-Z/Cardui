using Cardui.Api.Models;

namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// One transaction reduced to the fields income and spending need.
/// </summary>
public sealed record TransactionActivityValue(
    decimal Amount,
    bool Pending,
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    string? CategoryKey,
    string? GroupKey,
    FinancialRecordProvenance? Provenance = null);
