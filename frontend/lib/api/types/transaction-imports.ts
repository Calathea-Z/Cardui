/**
 * How a CSV writes a positive amount.
 * PositiveOut means money out. PositiveIn means money in.
 */
export type CsvAmountSign = "PositiveOut" | "PositiveIn";

/**
 * How a CSV writes a numeric date.
 * MonthFirst reads 01/02/2026 as January 2. DayFirst reads it as February 1.
 */
export type CsvDateOrder = "MonthFirst" | "DayFirst";

/**
 * Column indexes suggested for a CSV.
 * Null means that column was not found.
 */
export type TransactionImportSuggestedMapDto = {
  dateColumn: number | null;
  nameColumn: number | null;
  amountColumn: number | null;
  debitColumn: number | null;
  creditColumn: number | null;
  categoryColumn: number | null;
  notesColumn: number | null;
  amountSign: CsvAmountSign;
  dateOrder: CsvDateOrder;
};

export type TransactionImportInspectDto = {
  fileName: string;
  headers: string[];
  sampleRows: string[][];
  dataRowCount: number;
  suggested: TransactionImportSuggestedMapDto;
};

/**
 * One CSV row after mapping.
 * Ready rows can be imported. Duplicate rows are likely already saved. Error rows need a fix and cannot be imported.
 */
export type TransactionImportPreviewRowDto = {
  lineNumber: number;
  date: string | null;
  name: string | null;
  amount: number | null;
  categoryName: string | null;
  status: "Ready" | "Duplicate" | "Error";
  message: string | null;
};

export type TransactionImportPreviewDto = {
  readyCount: number;
  duplicateCount: number;
  errorCount: number;
  rows: TransactionImportPreviewRowDto[];
};

/**
 * A completed CSV import.
 * `undoneAt` is set after the batch is archived.
 */
export type TransactionImportBatchDto = {
  id: string;
  accountId: string;
  accountName: string;
  fileName: string;
  importedCount: number;
  createdAt: string;
  undoneAt: string | null;
};
