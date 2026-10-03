namespace Cardui.Api.Models;

public static class FinancialRecordSource
{
    public const int MaxLength = 32;

    public const string Plaid = "Plaid";

    public const string Manual = "Manual";
}

public static class FinancialRecordProvenance
{
    public const int MaxLength = 64;

    public const string PlaidSync = "PlaidSync";

    public const string ManualEntry = "ManualEntry";

    public const string BalanceReconciliation = "BalanceReconciliation";

    public const string BalanceReconciliationName = "Balance reconciliation";
}
