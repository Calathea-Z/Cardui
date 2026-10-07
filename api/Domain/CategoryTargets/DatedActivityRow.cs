using Cardui.Api.Domain.Transactions;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.CategoryTargets;

/// <summary>
/// One transaction row loaded for category targets, including the date used to place it in a month.
/// </summary>
internal sealed record DatedActivityRow(
    DateOnly Date,
    decimal Amount,
    bool Pending,
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    string? CategoryKey,
    string? GroupKey,
    FinancialRecordProvenance Provenance,
    string? CurrencyCode)
{
    /// <summary>
    /// Copies the row into the value the activity calculator totals.
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
            Provenance);
    }
}
