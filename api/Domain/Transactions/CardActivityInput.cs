using Cardui.Api.Models;

namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// The fields needed to tell a purchase, a refund, and a card payment apart.
/// Amount uses the stored sign: positive leaves the account, negative enters it.
/// AccountType is null when the account was not loaded. Cash and debt then stay unchanged.
/// </summary>
public sealed record CardActivityInput(
    string? AccountType,
    decimal Amount,
    bool Pending,
    FinancialRecordProvenance? Provenance,
    string? CategoryKey,
    string? GroupKey,
    string? MerchantName,
    string? Name);
