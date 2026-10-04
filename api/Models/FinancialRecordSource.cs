namespace Cardui.Api.Models;

/// <summary>
/// Where a financial row came from.
/// The stored column and the API use the member name.
/// </summary>
public enum FinancialRecordSource
{
    Plaid,
    Manual,
    Csv
}
