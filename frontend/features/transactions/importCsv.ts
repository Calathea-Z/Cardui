import type { TransactionImportSuggestedMapDto } from "@/lib/api/types";

/**
 * How a CSV writes money.
 * `amount` is one signed column. `split` is separate debit and credit columns.
 */
export type AmountMode = "amount" | "split";

/**
 * Largest CSV the import sheet will accept.
 * A larger file is rejected before it is read.
 */
export const maxCsvBytes = 1_048_576;

/**
 * Column mapping the import sheet sends to the API.
 * Empty strings mean that column is not used.
 */
export type ImportColumnState = {
  dateColumn: string;
  nameColumn: string;
  amountMode: AmountMode;
  amountColumn: string;
  debitColumn: string;
  creditColumn: string;
  categoryColumn: string;
  notesColumn: string;
  amountSign: string;
  dateOrder: string;
};

/**
 * Column mapping used before a file has been read.
 * A positive amount starts as money out, and dates start as month first.
 */
export const emptyImportColumns: ImportColumnState = {
  dateColumn: "",
  nameColumn: "",
  amountMode: "amount",
  amountColumn: "",
  debitColumn: "",
  creditColumn: "",
  categoryColumn: "",
  notesColumn: "",
  amountSign: "PositiveOut",
  dateOrder: "MonthFirst",
};

/**
 * Turns the API's suggested columns into the sheet's mapping.
 * Debit or credit without an amount column starts in split mode.
 */
export function columnStateFromSuggestion(
  suggested: TransactionImportSuggestedMapDto,
): ImportColumnState {
  const split =
    suggested.amountColumn === null &&
    (suggested.debitColumn !== null || suggested.creditColumn !== null);

  return {
    dateColumn: columnValue(suggested.dateColumn),
    nameColumn: columnValue(suggested.nameColumn),
    amountMode: split ? "split" : "amount",
    amountColumn: columnValue(suggested.amountColumn),
    debitColumn: columnValue(suggested.debitColumn),
    creditColumn: columnValue(suggested.creditColumn),
    categoryColumn: columnValue(suggested.categoryColumn),
    notesColumn: columnValue(suggested.notesColumn),
    amountSign: suggested.amountSign || "PositiveOut",
    dateOrder: suggested.dateOrder || "MonthFirst",
  };
}

/**
 * Builds the column list for a mapping select.
 * Optional columns include "Not used" so the household can leave them blank.
 */
export function columnChoices(headers: string[], includeUnused: boolean) {
  const columns = headers.map((header, index) => ({
    value: String(index),
    label: header.trim() || `Column ${index + 1}`,
  }));

  return includeUnused
    ? [{ value: "", label: "Not used" }, ...columns]
    : columns;
}

/**
 * Returns the reason a mapping cannot be previewed, or null when it can.
 * Date and name are required. Amount needs one column, or a debit, a credit, or both.
 */
export function mappingError(columns: ImportColumnState) {
  if (!columns.dateColumn || !columns.nameColumn) {
    return "Choose the date and name columns.";
  }

  if (columns.amountMode === "amount" && !columns.amountColumn) {
    return "Choose the amount column.";
  }

  if (
    columns.amountMode === "split" &&
    !columns.debitColumn &&
    !columns.creditColumn
  ) {
    return "Choose a debit column, a credit column, or both.";
  }

  return null;
}

/**
 * Import sheet steps, in the order the household walks them.
 */
export const importSteps = ["account", "columns", "rows", "import"] as const;

/** One step in the import sheet. */
export type ImportStep = (typeof importSteps)[number];

/** Which import request is in flight. Null means the sheet is idle. */
export type ImportBusy = "inspect" | "preview" | "import" | "undo" | null;

const importStepHeadings: Record<ImportStep, string> = {
  account: "Choose the account and file",
  columns: "Map columns",
  rows: "Preview and choose rows",
  import: "Import",
};

/** Heading shown for the current import step. */
export function importStepHeading(step: ImportStep) {
  return importStepHeadings[step];
}

/**
 * The step before this one, or null on the first step.
 */
export function previousImportStep(step: ImportStep) {
  return adjacentImportStep(step, -1);
}

/**
 * The step after this one, or null on the last step.
 */
export function nextImportStep(step: ImportStep) {
  return adjacentImportStep(step, 1);
}

/**
 * Adds the file, account, and column mapping to a preview or import request.
 * Unused columns are omitted. Included line numbers limit which preview rows are imported.
 */
export function appendTransactionImport(
  form: FormData,
  file: File,
  accountId: string,
  columns: ImportColumnState,
  includedLineNumbers?: number[],
) {
  form.append("file", file);
  form.append("accountId", accountId);
  form.append("dateColumn", columns.dateColumn);
  form.append("nameColumn", columns.nameColumn);
  form.append("amountSign", columns.amountSign);
  form.append("dateOrder", columns.dateOrder);

  if (columns.amountMode === "amount") {
    appendColumn(form, "amountColumn", columns.amountColumn);
  } else {
    appendColumn(form, "debitColumn", columns.debitColumn);
    appendColumn(form, "creditColumn", columns.creditColumn);
  }

  appendColumn(form, "categoryColumn", columns.categoryColumn);
  appendColumn(form, "notesColumn", columns.notesColumn);

  if (includedLineNumbers) {
    form.append("includedLineNumbers", includedLineNumbers.join(","));
  }
}

/**
 * Moves one step forward or back.
 * Returns null at either end so the sheet can hide that button.
 */
function adjacentImportStep(step: ImportStep, direction: -1 | 1) {
  const index = importSteps.indexOf(step);
  const next = index + direction;
  if (index < 0 || next < 0 || next >= importSteps.length) {
    return null;
  }

  return importSteps[next];
}

/**
 * Stores a suggested column index as the select value.
 * A missing column is an empty string, which means not used.
 */
function columnValue(column: number | null) {
  return column === null ? "" : String(column);
}

/**
 * Adds a column index to the request when the household chose one.
 */
function appendColumn(form: FormData, name: string, value: string) {
  if (value) {
    form.append(name, value);
  }
}
