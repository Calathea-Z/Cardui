using Cardui.Api.Models;

namespace Cardui.Api.Domain.Obligations;

/// <summary>
/// One posted activity row the recurring-bill finder can consider.
/// A positive amount is money leaving the account. Income is negative and is not a bill.
/// </summary>
public sealed record RecurringSuggestionCharge(
    DateOnly Date,
    decimal Amount,
    string Name,
    string? MerchantName,
    Guid AccountId,
    string? AccountName,
    string? Currency,
    bool Pending,
    bool Archived,
    string? CategoryKey,
    string? GroupKey,
    FinancialRecordProvenance Provenance);
