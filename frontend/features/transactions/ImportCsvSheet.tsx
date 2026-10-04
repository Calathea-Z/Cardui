"use client";

import { useEffect, useRef, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { cn } from "@/lib/utils";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { Select, type SelectOption } from "@/components/ui/select";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import {
  commitTransactionImport,
  inspectTransactionImport,
  listTransactionImports,
  previewTransactionImport,
  undoTransactionImport,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type {
  AccountDto,
  TransactionImportBatchDto,
  TransactionImportInspectDto,
  TransactionImportPreviewDto,
  TransactionImportPreviewRowDto,
} from "@/lib/api/types";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";
import {
  appendTransactionImport,
  columnChoices,
  columnStateFromSuggestion,
  importStepHeading,
  importSteps,
  mappingError,
  nextImportStep,
  previousImportStep,
  type ImportColumnState,
  type ImportStep,
} from "./importCsv";

/**
 * Largest CSV the import sheet will accept.
 * A larger file is rejected before it is read.
 */
const maxCsvBytes = 1_048_576;

/**
 * Column mapping used before a file has been read.
 * A positive amount starts as money out, and dates start as month first.
 */
const emptyColumns: ImportColumnState = {
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

type ImportBusy = "inspect" | "preview" | "import" | "undo" | null;

type ImportCsvSheetProps = {
  open: boolean;
  accounts: AccountDto[];
  onClose: () => void;
  onImported: () => void;
};

/**
 * Writes a preview amount as money in or money out.
 * A missing amount is blank, a negative amount reads as money in, and zero or a positive amount reads as money out.
 */
function formatImportAmount(amount: number | null, currency: string | null) {
  if (amount === null) {
    return "";
  }

  const formatted = formatCurrency(Math.abs(amount), currency);
  return amount < 0 ? `Money in ${formatted}` : `Money out ${formatted}`;
}

/**
 * Writes an import batch's created date as a short US date.
 * An unreadable value is blank.
 */
function formatBatchDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return date.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

/**
 * Writes a row count as "transaction" or "transactions".
 */
function transactionLabel(count: number) {
  return `${count} transaction${count === 1 ? "" : "s"}`;
}

/**
 * Lists the four import steps and marks the current one.
 * Steps already passed use the foreground color, and later steps stay muted.
 */
function ImportStepIndicator({ step }: { step: ImportStep }) {
  const currentIndex = importSteps.indexOf(step);

  return (
    <ol aria-label="Import steps" className="flex flex-col gap-1">
      {importSteps.map((item, index) => {
        const current = item === step;
        const label = `${index + 1}. ${importStepHeading(item)}`;
        return (
          <li key={item} aria-current={current ? "step" : undefined}>
            {current ? (
              <h3 className="text-base font-semibold text-foreground">
                {label}
              </h3>
            ) : (
              <p
                className={
                  index < currentIndex
                    ? "text-sm text-foreground"
                    : "text-sm text-muted-foreground"
                }
              >
                {label}
              </p>
            )}
          </li>
        );
      })}
    </ol>
  );
}

/**
 * Lists imports the household can still undo.
 * Undo archives every transaction from that file, and an empty list renders nothing.
 */
function OpenImportList({
  batches,
  pendingUndoId,
  busy,
  onAsk,
  onCancel,
  onConfirm,
}: {
  batches: TransactionImportBatchDto[];
  pendingUndoId: string | null;
  busy: ImportBusy;
  onAsk: (importId: string) => void;
  onCancel: () => void;
  onConfirm: (importId: string) => void;
}) {
  const isWorking = busy !== null;
  if (batches.length === 0) {
    return null;
  }

  return (
    <section className="flex flex-col gap-2 border-t border-border pt-4">
      <h3 className="text-sm font-medium">Undo an import</h3>
      {batches.map((batch) => (
        <div
          key={batch.id}
          className="flex flex-col gap-2 rounded-lg border border-border px-3 py-2"
        >
          <p className="text-sm">
            {batch.fileName} · {batch.importedCount} on {batch.accountName} ·{" "}
            {formatBatchDate(batch.createdAt)}
          </p>
          {pendingUndoId === batch.id ? (
            <div className="flex flex-col gap-2">
              <p className="text-xs text-muted-foreground">
                This archives every transaction from that file.
              </p>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="destructive"
                  disabled={isWorking}
                  onClick={() => onConfirm(batch.id)}
                >
                  {busy === "undo"
                    ? "Archiving"
                    : "Archive imported transactions"}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  disabled={isWorking}
                  onClick={onCancel}
                >
                  Cancel
                </Button>
              </div>
            </div>
          ) : (
            <Button
              type="button"
              variant="outline"
              className="self-start"
              disabled={isWorking}
              onClick={() => onAsk(batch.id)}
            >
              Undo
            </Button>
          )}
        </div>
      ))}
    </section>
  );
}

/**
 * Renders a labeled choice list on the import sheet.
 * An optional hint is shown under the list.
 */
function LabeledSelect({
  label,
  title,
  value,
  onChange,
  onOpenChange,
  options,
  placeholder,
  hint,
}: {
  label: string;
  title: string;
  value: string;
  onChange: (value: string) => void;
  onOpenChange: (open: boolean) => void;
  options: SelectOption[];
  placeholder?: string;
  hint?: string;
}) {
  return (
    <div className="flex flex-col gap-1.5 text-sm font-medium">
      {label}
      <Select
        title={title}
        value={value}
        onChange={onChange}
        onOpenChange={onOpenChange}
        placeholder={placeholder}
        className="h-9"
        options={options}
      />
      {hint ? (
        <p className="text-xs font-normal text-muted-foreground">{hint}</p>
      ) : null}
    </div>
  );
}

/**
 * Shows sample rows from the inspected file.
 * A blank header is labeled by column number, and an empty sample renders nothing.
 */
function SampleRows({ inspect }: { inspect: TransactionImportInspectDto }) {
  if (inspect.sampleRows.length === 0) {
    return null;
  }

  return (
    <figure className="flex flex-col gap-2">
      <figcaption className="text-sm font-medium">
        Sample from the file
      </figcaption>
      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full border-collapse text-left text-xs">
          <thead className="bg-muted/40">
            <tr>
              {inspect.headers.map((header, index) => (
                <th
                  key={`${header}-${index}`}
                  className="border-b border-border px-3 py-2 font-medium whitespace-nowrap"
                >
                  {header.trim() || `Column ${index + 1}`}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {inspect.sampleRows.map((row, rowIndex) => (
              <tr key={rowIndex} className="border-b border-border">
                {inspect.headers.map((_, cellIndex) => (
                  <td key={cellIndex} className="px-3 py-2 whitespace-nowrap">
                    {row[cellIndex] ?? ""}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </figure>
  );
}

/**
 * Lets the household map columns and say how dates and amounts are written.
 * Debit is money out and credit is money in, and the positive-amount choice applies to a single amount column.
 */
function ColumnMapping({
  headers,
  columns,
  onChange,
  onOpenChange,
}: {
  headers: string[];
  columns: ImportColumnState;
  onChange: (next: ImportColumnState) => void;
  onOpenChange: (open: boolean) => void;
}) {
  const requiredColumns = columnChoices(headers, false);
  const optionalColumns = columnChoices(headers, true);

  return (
    <div className="flex flex-col gap-5">
      <section className="flex flex-col gap-3">
        <div className="flex flex-col gap-1">
          <h3 className="text-sm font-medium">Which column is which</h3>
          <p className="text-xs font-normal text-muted-foreground">
            Choose the column from the sample that holds each part of the
            transaction.
          </p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <LabeledSelect
            label="Date is in"
            title="Date column"
            value={columns.dateColumn}
            onChange={(value) => onChange({ ...columns, dateColumn: value })}
            onOpenChange={onOpenChange}
            placeholder="Choose a column"
            options={requiredColumns}
          />
          <LabeledSelect
            label="Name is in"
            title="Name column"
            value={columns.nameColumn}
            onChange={(value) => onChange({ ...columns, nameColumn: value })}
            onOpenChange={onOpenChange}
            placeholder="Choose a column"
            options={requiredColumns}
          />
          {columns.amountMode === "amount" ? (
            <LabeledSelect
              label="Amount is in"
              title="Amount column"
              value={columns.amountColumn}
              onChange={(value) =>
                onChange({ ...columns, amountColumn: value })
              }
              onOpenChange={onOpenChange}
              placeholder="Choose a column"
              options={requiredColumns}
            />
          ) : (
            <>
              <LabeledSelect
                label="Debit is in"
                title="Debit column"
                value={columns.debitColumn}
                onChange={(value) =>
                  onChange({ ...columns, debitColumn: value })
                }
                onOpenChange={onOpenChange}
                placeholder="Not used"
                options={optionalColumns}
              />
              <LabeledSelect
                label="Credit is in"
                title="Credit column"
                value={columns.creditColumn}
                onChange={(value) =>
                  onChange({ ...columns, creditColumn: value })
                }
                onOpenChange={onOpenChange}
                placeholder="Not used"
                options={optionalColumns}
              />
            </>
          )}
          <LabeledSelect
            label="Category is in"
            title="Category column"
            value={columns.categoryColumn}
            onChange={(value) =>
              onChange({ ...columns, categoryColumn: value })
            }
            onOpenChange={onOpenChange}
            placeholder="Not used"
            options={optionalColumns}
          />
          <LabeledSelect
            label="Notes are in"
            title="Notes column"
            value={columns.notesColumn}
            onChange={(value) => onChange({ ...columns, notesColumn: value })}
            onOpenChange={onOpenChange}
            placeholder="Not used"
            options={optionalColumns}
          />
        </div>
      </section>
      <section className="flex flex-col gap-3">
        <div className="flex flex-col gap-1">
          <h3 className="text-sm font-medium">How to read the values</h3>
          <p className="text-xs font-normal text-muted-foreground">
            These choices describe how a date or an amount is written.
          </p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <LabeledSelect
            label="Amounts are written as"
            title="How amounts are written"
            value={columns.amountMode}
            onChange={(value) =>
              onChange({
                ...columns,
                amountMode: value === "split" ? "split" : "amount",
              })
            }
            onOpenChange={onOpenChange}
            options={[
              { value: "amount", label: "One amount column" },
              { value: "split", label: "Debit and credit columns" },
            ]}
          />
          <LabeledSelect
            label="Dates are written"
            title="How dates are written"
            value={columns.dateOrder}
            onChange={(value) => onChange({ ...columns, dateOrder: value })}
            onOpenChange={onOpenChange}
            hint="Month first reads 01/02/2026 as January 2. Day first reads it as February 1. A date like 2026-10-01 is the same either way."
            options={[
              { value: "MonthFirst", label: "Month first" },
              { value: "DayFirst", label: "Day first" },
            ]}
          />
          {columns.amountMode === "amount" ? (
            <LabeledSelect
              label="A positive amount is"
              title="What a positive amount means"
              value={columns.amountSign}
              onChange={(value) => onChange({ ...columns, amountSign: value })}
              onOpenChange={onOpenChange}
              hint="Money out was spent. Money in was received."
              options={[
                { value: "PositiveOut", label: "Money out" },
                { value: "PositiveIn", label: "Money in" },
              ]}
            />
          ) : null}
        </div>
        <p className="text-xs text-muted-foreground">
          Debit is money out and credit is money in. A CR or DR marker sets the
          direction on its own. Money in counts as income only when the category
          is Income.
        </p>
      </section>
    </div>
  );
}

/**
 * Lists preview rows so the household can include or skip each line.
 * A row that needs a fix cannot be checked.
 */
function PreviewRows({
  rows,
  included,
  currency,
  isWorking,
  onToggle,
}: {
  rows: TransactionImportPreviewRowDto[];
  included: Set<number>;
  currency: string | null;
  isWorking: boolean;
  onToggle: (lineNumber: number) => void;
}) {
  return (
    <ul className="flex flex-col">
      {rows.map((row) => {
        const disabled = row.status === "Error";
        return (
          <li key={row.lineNumber} className="border-b border-border py-3">
            <label className="flex gap-3">
              <input
                type="checkbox"
                className="mt-1 size-4 accent-primary"
                checked={included.has(row.lineNumber)}
                disabled={disabled || isWorking}
                aria-label={`Include line ${row.lineNumber} ${row.name ?? ""}`}
                onChange={() => onToggle(row.lineNumber)}
              />
              <span className="min-w-0 flex-1">
                <span className="flex flex-wrap items-baseline justify-between gap-2">
                  <span className="font-medium">{row.name || "Untitled"}</span>
                  <span className="text-sm">
                    {formatImportAmount(row.amount, currency)}
                  </span>
                </span>
                <span className="mt-1 block text-xs text-muted-foreground">
                  Line {row.lineNumber}
                  {row.date ? ` · ${row.date}` : ""}
                  {row.categoryName ? ` · ${row.categoryName}` : ""}
                  {row.status === "Duplicate" ? " · Duplicate" : ""}
                  {row.status === "Error" ? " · Needs a fix" : ""}
                </span>
                {row.message ? (
                  <span className="mt-1 block text-xs text-muted-foreground">
                    {row.message}
                  </span>
                ) : null}
              </span>
            </label>
          </li>
        );
      })}
    </ul>
  );
}

/**
 * Walks the household through choosing a file, mapping columns, choosing rows, and importing.
 * Back keeps the account, file, mapping, and row choices, and the file must be a .csv of at most 1 MB.
 */
function ImportCsvForm({
  accounts,
  onImported,
  onPickerOpenChange,
}: Omit<ImportCsvSheetProps, "open" | "onClose"> & {
  onPickerOpenChange: (open: boolean) => void;
}) {
  const openAccounts = accounts.filter((account) => !account.archivedAt);
  const [step, setStep] = useState<ImportStep>("account");
  const [accountId, setAccountId] = useState(openAccounts[0]?.id ?? "");
  const [file, setFile] = useState<File | null>(null);
  const [fileKey, setFileKey] = useState(0);
  const [inspect, setInspect] = useState<TransactionImportInspectDto | null>(
    null,
  );
  const [columns, setColumns] = useState<ImportColumnState>(emptyColumns);
  const [preview, setPreview] = useState<TransactionImportPreviewDto | null>(
    null,
  );
  const [included, setIncluded] = useState<Set<number>>(new Set());
  const [openImports, setOpenImports] = useState<TransactionImportBatchDto[]>(
    [],
  );
  const [pendingUndoId, setPendingUndoId] = useState<string | null>(null);
  const [result, setResult] = useState<TransactionImportBatchDto | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState<ImportBusy>(null);
  const inspectRequest = useRef(0);
  const previewRequest = useRef(0);
  const isWorking = busy !== null;

  const selectedAccount = openAccounts.find(
    (account) => account.id === accountId,
  );
  const selectedRows = preview
    ? preview.rows.filter((row) => included.has(row.lineNumber))
    : [];
  const includedCount = selectedRows.length;
  const includedDuplicates = selectedRows.filter(
    (row) => row.status === "Duplicate",
  ).length;
  const undoBatches =
    result && !openImports.some((batch) => batch.id === result.id)
      ? [result, ...openImports]
      : openImports;

  useEffect(() => {
    let cancelled = false;
    void listTransactionImports()
      .then((imports) => {
        if (!cancelled) {
          setOpenImports(imports);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setOpenImports([]);
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  /**
   * Stores a new column mapping and clears the current preview.
   * A preview request still in flight is ignored.
   */
  function changeColumns(next: ImportColumnState) {
    previewRequest.current += 1;
    setColumns(next);
    setPreview(null);
    setIncluded(new Set());
    setBusy((current) => (current === "preview" ? null : current));
  }

  /**
   * Drops the chosen file and shows why it was rejected.
   * An inspect still in flight is ignored.
   */
  function clearFile(message: string) {
    inspectRequest.current += 1;
    setBusy((current) => (current === "inspect" ? null : current));
    setFile(null);
    setFileKey((current) => current + 1);
    setErrorMessage(message);
  }

  /**
   * Reloads the imports that can still be undone.
   */
  async function refreshOpenImports() {
    setOpenImports(await listTransactionImports());
  }

  /**
   * Reads a chosen CSV and applies the suggested column mapping.
   * The file must end in .csv and be at most 1 MB, and a newer file ignores an older read.
   */
  async function onFileSelected(next: File | null) {
    setResult(null);
    setPreview(null);
    setIncluded(new Set());
    setErrorMessage(null);
    setInspect(null);
    setFile(next);

    if (!next) {
      inspectRequest.current += 1;
      setBusy((current) => (current === "inspect" ? null : current));
      return;
    }

    if (!next.name.toLowerCase().endsWith(".csv")) {
      clearFile("Choose a .csv file.");
      return;
    }

    if (next.size > maxCsvBytes) {
      clearFile("The CSV file must be 1 MB or smaller.");
      return;
    }

    const request = ++inspectRequest.current;
    setBusy("inspect");
    try {
      const inspected = await inspectTransactionImport(next);
      if (request !== inspectRequest.current) {
        return;
      }

      setInspect(inspected);
      setColumns(columnStateFromSuggestion(inspected.suggested));
    } catch (error) {
      if (request !== inspectRequest.current) {
        return;
      }

      setFile(null);
      setFileKey((current) => current + 1);
      setErrorMessage(getApiErrorMessage(error, "Could not read this CSV."));
    } finally {
      if (request === inspectRequest.current) {
        setBusy(null);
      }
    }
  }

  /**
   * Moves to the next import step.
   * The last step stays where it is.
   */
  function goToNext(from: ImportStep) {
    const next = nextImportStep(from);
    if (!next) {
      return;
    }

    setErrorMessage(null);
    setStep(next);
  }

  /**
   * Leaves the account step when an account, a file, and a successful read are present.
   */
  function continueFromAccount() {
    if (!file || !accountId || !inspect) {
      setErrorMessage("Choose an account and a CSV file.");
      return;
    }

    goToNext("account");
  }

  /**
   * Checks the column mapping and previews the file before the row step.
   * Ready rows start checked, duplicates stay unchecked, and a preview already loaded moves on without another request.
   */
  async function previewAndContinue() {
    if (!file || !accountId) {
      setErrorMessage("Choose an account and a CSV file.");
      setStep("account");
      return;
    }

    const message = mappingError(columns);
    if (message) {
      setErrorMessage(message);
      return;
    }

    if (preview) {
      goToNext("columns");
      return;
    }

    const request = ++previewRequest.current;
    setBusy("preview");
    setErrorMessage(null);
    setResult(null);
    try {
      const form = new FormData();
      appendTransactionImport(form, file, accountId, columns);
      const nextPreview = await previewTransactionImport(form);
      if (request !== previewRequest.current) {
        return;
      }

      setPreview(nextPreview);
      setIncluded(
        new Set(
          nextPreview.rows
            .filter((row) => row.status === "Ready")
            .map((row) => row.lineNumber),
        ),
      );
      goToNext("columns");
    } catch (error) {
      if (request !== previewRequest.current) {
        return;
      }

      setErrorMessage(getApiErrorMessage(error, "Could not preview this CSV."));
    } finally {
      if (request === previewRequest.current) {
        setBusy(null);
      }
    }
  }

  /**
   * Leaves the row step when at least one row is included.
   */
  function continueFromRows() {
    if (includedCount === 0) {
      setErrorMessage("Choose at least one row to import.");
      return;
    }

    goToNext("rows");
  }

  /**
   * Returns to the previous import step and keeps the account, file, mapping, and row choices.
   * Back stays put on the first step and after a finished import.
   */
  function goBack() {
    if (result) {
      return;
    }

    const previous = previousImportStep(step);
    if (!previous) {
      return;
    }

    setErrorMessage(null);
    setStep(previous);
  }

  /**
   * Imports the included lines onto the chosen account.
   * The file is cleared afterward, the undo list reloads, and the transactions page is asked to refresh.
   */
  async function importSelected() {
    if (!file || !preview) {
      return;
    }

    const lineNumbers = selectedRows.map((row) => row.lineNumber);
    if (lineNumbers.length === 0) {
      setErrorMessage("Choose at least one row to import.");
      setStep("rows");
      return;
    }

    setBusy("import");
    setErrorMessage(null);
    try {
      const form = new FormData();
      appendTransactionImport(form, file, accountId, columns, lineNumbers);
      const imported = await commitTransactionImport(form);
      setResult(imported);
      setPreview(null);
      setInspect(null);
      setFile(null);
      setFileKey((current) => current + 1);
      await refreshOpenImports();
      onImported();
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Could not import this CSV."));
    } finally {
      setBusy(null);
    }
  }

  /**
   * Archives every transaction from the chosen import.
   * Undoing the import that just finished returns the sheet to the account step.
   */
  async function confirmUndo(importId: string) {
    setBusy("undo");
    setErrorMessage(null);
    try {
      await undoTransactionImport(importId);
      setPendingUndoId(null);
      if (result?.id === importId) {
        setResult(null);
        setStep("account");
      }
      await refreshOpenImports();
      onImported();
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Could not undo this import."));
    } finally {
      setBusy(null);
    }
  }

  /**
   * Includes or drops one preview line.
   * Any message from the previous action is cleared.
   */
  function toggleRow(lineNumber: number) {
    setErrorMessage(null);
    setIncluded((current) => {
      const next = new Set(current);
      if (next.has(lineNumber)) {
        next.delete(lineNumber);
      } else {
        next.add(lineNumber);
      }
      return next;
    });
  }

  /**
   * Returns to the first step for another file.
   * The chosen account stays. The file, mapping, preview, and row choices are cleared.
   */
  function startAnother() {
    setResult(null);
    setPreview(null);
    setIncluded(new Set());
    setInspect(null);
    setFile(null);
    setColumns(emptyColumns);
    setErrorMessage(null);
    setPendingUndoId(null);
    setStep("account");
  }

  const leftOut = preview ? preview.rows.length - includedCount : 0;

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-5">
      <ImportStepIndicator step={result ? "import" : step} />

      {step === "account" && openAccounts.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          Add an account before importing a CSV. You can still add a transaction
          by hand.
        </p>
      ) : null}

      {step === "account" && openAccounts.length > 0 ? (
        <>
          <LabeledSelect
            label="Account"
            title="Account"
            value={accountId}
            onChange={(value) => {
              setAccountId(value);
              setPreview(null);
              setIncluded(new Set());
              setErrorMessage(null);
            }}
            onOpenChange={onPickerOpenChange}
            options={openAccounts.map((account) => ({
              value: account.id,
              label: account.name,
            }))}
          />
          {selectedAccount?.plaidItemId ? (
            <p className="text-xs text-muted-foreground">
              This account is linked. The bank still supplies its balance. The
              imported transactions count in activity.
            </p>
          ) : null}
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            CSV file
            <input
              key={fileKey}
              type="file"
              accept=".csv,text/csv"
              className="text-sm file:mr-3 file:rounded-lg file:border-0 file:bg-secondary file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-secondary-foreground"
              onChange={(event) => {
                void onFileSelected(event.target.files?.[0] ?? null);
              }}
            />
          </label>
          {busy === "inspect" ? (
            <p className="text-sm text-muted-foreground">Reading the file.</p>
          ) : null}
          {inspect ? (
            <p className="text-sm text-muted-foreground">
              {inspect.dataRowCount} transactions in {inspect.fileName}.
            </p>
          ) : null}
          <p className="text-xs text-muted-foreground">
            Manual entry stays available. Import adds posted transactions.
          </p>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              disabled={isWorking}
              onClick={continueFromAccount}
            >
              Continue
            </Button>
          </div>
        </>
      ) : null}

      {step === "columns" && inspect ? (
        <>
          <p className="text-sm text-muted-foreground">
            This says which column becomes the date, the name, and the amount.
            The column names from the file are already selected. Change one when
            it points at the wrong column.
          </p>
          <p className="text-sm text-muted-foreground">
            {inspect.dataRowCount} transactions in {inspect.fileName}.
          </p>
          <SampleRows inspect={inspect} />
          <ColumnMapping
            headers={inspect.headers}
            columns={columns}
            onChange={changeColumns}
            onOpenChange={onPickerOpenChange}
          />
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={isWorking}
              onClick={goBack}
            >
              Back
            </Button>
            <Button
              type="button"
              disabled={isWorking}
              onClick={() => void previewAndContinue()}
            >
              {busy === "preview"
                ? "Working"
                : preview
                  ? "Continue"
                  : "Preview"}
            </Button>
          </div>
        </>
      ) : null}

      {step === "rows" && preview ? (
        <>
          <p className="text-sm">
            {preview.readyCount} ready · {preview.duplicateCount} duplicates ·{" "}
            {preview.errorCount} to fix
          </p>
          <p className="text-xs text-muted-foreground">
            Ready rows are checked. A duplicate stays unchecked until you
            include it. A row that needs a fix cannot be imported.
          </p>
          <PreviewRows
            rows={preview.rows}
            included={included}
            currency={selectedAccount?.isoCurrencyCode ?? null}
            isWorking={isWorking}
            onToggle={toggleRow}
          />
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={isWorking}
              onClick={goBack}
            >
              Back
            </Button>
            <Button
              type="button"
              disabled={isWorking || includedCount === 0}
              onClick={continueFromRows}
            >
              Continue
            </Button>
          </div>
        </>
      ) : null}

      {step === "import" && preview && !result ? (
        <>
          <p className="text-sm">
            Import {transactionLabel(includedCount)} onto{" "}
            {selectedAccount?.name ?? "this account"} from{" "}
            {inspect?.fileName ?? "this file"}.
          </p>
          <p className="text-sm text-muted-foreground">
            {includedCount - includedDuplicates} ready
            {includedDuplicates > 0
              ? ` · ${includedDuplicates} duplicate${includedDuplicates === 1 ? "" : "s"} included`
              : ""}
            {leftOut > 0 ? ` · ${leftOut} not included` : ""}
          </p>
          {selectedAccount?.plaidItemId ? (
            <p className="text-xs text-muted-foreground">
              This account is linked. The bank still supplies its balance. The
              imported transactions count in activity.
            </p>
          ) : null}
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={isWorking}
              onClick={goBack}
            >
              Back
            </Button>
            <Button
              type="button"
              size="lg"
              disabled={isWorking || includedCount === 0}
              onClick={() => void importSelected()}
            >
              {busy === "import"
                ? "Importing"
                : `Import ${transactionLabel(includedCount)}`}
            </Button>
          </div>
        </>
      ) : null}

      {result ? (
        <>
          <Alert>
            Imported {result.importedCount} from {result.fileName}. Undo is
            below until you archive that batch.
          </Alert>
          <OpenImportList
            batches={undoBatches}
            pendingUndoId={pendingUndoId}
            busy={busy}
            onAsk={setPendingUndoId}
            onCancel={() => setPendingUndoId(null)}
            onConfirm={(importId) => void confirmUndo(importId)}
          />
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={isWorking}
              onClick={startAnother}
            >
              Import another file
            </Button>
          </div>
        </>
      ) : null}

      {errorMessage ? (
        <Alert variant="destructive">{errorMessage}</Alert>
      ) : null}

      {step === "account" && !result ? (
        <OpenImportList
          batches={openImports}
          pendingUndoId={pendingUndoId}
          busy={busy}
          onAsk={setPendingUndoId}
          onCancel={() => setPendingUndoId(null)}
          onConfirm={(importId) => void confirmUndo(importId)}
        />
      ) : null}
    </div>
  );
}

/**
 * Opens the CSV import sheet.
 * Escape is ignored while a column or account list is open, and the form remounts each time the sheet opens.
 */
export function ImportCsvSheet({
  open,
  accounts,
  onClose,
  onImported,
}: ImportCsvSheetProps) {
  const [pickerOpen, setPickerOpen] = useState(false);

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title="Import CSV"
      className={cn(
        FULL_SCREEN_SHEET_CLASSNAME,
        "md:inset-x-auto md:left-1/2 md:w-full md:max-w-3xl md:-translate-x-1/2",
      )}
      closeOnEscape={!pickerOpen}
    >
      {open ? (
        <ImportCsvForm
          accounts={accounts}
          onImported={onImported}
          onPickerOpenChange={setPickerOpen}
        />
      ) : null}
    </BottomSheet>
  );
}
