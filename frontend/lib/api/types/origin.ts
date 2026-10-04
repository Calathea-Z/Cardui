/**
 * Where a financial row came from.
 * The screen branches on these values. A new source belongs in this union.
 */
export type FinancialRecordSource = "Plaid" | "Manual" | "Csv";

/**
 * How a financial row was created.
 * The screen branches on these values. A new provenance belongs in this union.
 */
export type FinancialRecordProvenance =
  "PlaidSync" | "ManualEntry" | "CsvImport" | "BalanceReconciliation";
