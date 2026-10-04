namespace Cardui.Api.Domain;

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
    string? Provenance,
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
