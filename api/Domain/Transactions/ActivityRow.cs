using Cardui.Api.Models;

namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// One transaction row loaded for the monthly activity total.
/// </summary>
internal sealed record ActivityRow(
    decimal Amount,
    bool Pending,
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    string? CategoryKey,
    string? GroupKey,
    FinancialRecordProvenance Provenance,
    string? CurrencyCode,
    string? AccountType,
    string? MerchantName,
    string? Name)
{
    /// <summary>
    /// Copies the row into the value the activity calculator totals.
    /// The account type and description let a card payment stay out of spending.
    /// </summary>
    public TransactionActivityValue ToValue()
    {
        return new TransactionActivityValue(
            Amount,
            Pending,
            CategoryId,
            CategoryName,
            CategoryColor,
            CategoryKey,
            GroupKey,
            Provenance,
            AccountType,
            MerchantName,
            Name);
    }
}
