namespace Cardui.Api.Domain.TransactionImport;

/// <summary>
/// How a positive amount in one amount column is read.
/// PositiveOut means money left the account.
/// </summary>
public enum CsvAmountSign
{
    PositiveOut,
    PositiveIn
}
