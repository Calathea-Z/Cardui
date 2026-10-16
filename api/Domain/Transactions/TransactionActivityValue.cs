using Cardui.Api.Models;

namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// One transaction reduced to the fields income and spending need.
/// AccountType, MerchantName, and Name let a card payment be left out of spending.
/// A null account type still classifies the description, and then leaves cash and debt unchanged.
/// </summary>
public sealed record TransactionActivityValue(
    decimal Amount,
    bool Pending,
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    string? CategoryKey,
    string? GroupKey,
    FinancialRecordProvenance? Provenance = null,
    string? AccountType = null,
    string? MerchantName = null,
    string? Name = null);
