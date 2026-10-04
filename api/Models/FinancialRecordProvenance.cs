namespace Cardui.Api.Models;

/// <summary>
/// How a financial row was created.
/// The stored column and the API use the member name.
/// </summary>
public enum FinancialRecordProvenance
{
    PlaidSync,
    ManualEntry,
    CsvImport,
    BalanceReconciliation
}
