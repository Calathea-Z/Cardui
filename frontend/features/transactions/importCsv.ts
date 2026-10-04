import type { TransactionImportSuggestedMapDto } from "@/lib/api/types";

export type AmountMode = "amount" | "split";

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

export function columnChoices(headers: string[], includeUnused: boolean) {
  const columns = headers.map((header, index) => ({
    value: String(index),
    label: header.trim() || `Column ${index + 1}`,
  }));

  return includeUnused
    ? [{ value: "", label: "Not used" }, ...columns]
    : columns;
}

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

export const importSteps = ["account", "columns", "rows", "import"] as const;

export type ImportStep = (typeof importSteps)[number];

const importStepHeadings: Record<ImportStep, string> = {
  account: "Choose the account and file",
  columns: "Map columns",
  rows: "Preview and choose rows",
  import: "Import",
};

export function importStepHeading(step: ImportStep) {
  return importStepHeadings[step];
}

export function previousImportStep(step: ImportStep) {
  return adjacentImportStep(step, -1);
}

export function nextImportStep(step: ImportStep) {
  return adjacentImportStep(step, 1);
}

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

function adjacentImportStep(step: ImportStep, direction: -1 | 1) {
  const index = importSteps.indexOf(step);
  const next = index + direction;
  if (index < 0 || next < 0 || next >= importSteps.length) {
    return null;
  }

  return importSteps[next];
}

function columnValue(column: number | null) {
  return column === null ? "" : String(column);
}

function appendColumn(form: FormData, name: string, value: string) {
  if (value) {
    form.append(name, value);
  }
}
