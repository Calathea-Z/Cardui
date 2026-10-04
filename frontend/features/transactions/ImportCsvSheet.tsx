"use client";

import { useEffect, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { cn } from "@/lib/utils";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { Select } from "@/components/ui/select";
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
} from "@/lib/api/types";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";
import {
  appendTransactionImport,
  columnChoices,
  columnStateFromSuggestion,
  mappingError,
  type ImportColumnState,
} from "./importCsv";

const maxCsvBytes = 1_048_576;

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

type ImportCsvSheetProps = {
  open: boolean;
  accounts: AccountDto[];
  onClose: () => void;
  onImported: () => void;
};

function formatImportAmount(amount: number | null, currency: string | null) {
  if (amount === null) {
    return "";
  }

  const formatted = formatCurrency(Math.abs(amount), currency);
  return amount < 0 ? `Money in ${formatted}` : `Money out ${formatted}`;
}

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

function ImportCsvForm({
  accounts,
  onImported,
  onPickerOpenChange,
}: Omit<ImportCsvSheetProps, "open" | "onClose"> & {
  onPickerOpenChange: (open: boolean) => void;
}) {
  const openAccounts = accounts.filter((account) => !account.archivedAt);
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
  const [busy, setBusy] = useState<
    "inspect" | "preview" | "import" | "undo" | null
  >(null);
  const isWorking = busy !== null;

  const selectedAccount = openAccounts.find(
    (account) => account.id === accountId,
  );

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

  function changeColumns(next: ImportColumnState) {
    setColumns(next);
    setPreview(null);
    setIncluded(new Set());
  }

  async function refreshOpenImports() {
    setOpenImports(await listTransactionImports());
  }

  async function onFileSelected(next: File | null) {
    setResult(null);
    setPreview(null);
    setIncluded(new Set());
    setErrorMessage(null);
    setInspect(null);
    setFile(next);

    if (!next) {
      return;
    }

    if (!next.name.toLowerCase().endsWith(".csv")) {
      setFile(null);
      setFileKey((current) => current + 1);
      setErrorMessage("Choose a .csv file.");
      return;
    }

    if (next.size > maxCsvBytes) {
      setFile(null);
      setFileKey((current) => current + 1);
      setErrorMessage("The CSV file must be 1 MB or smaller.");
      return;
    }

    setBusy("inspect");
    try {
      const inspected = await inspectTransactionImport(next);
      setInspect(inspected);
      setColumns(columnStateFromSuggestion(inspected.suggested));
    } catch (error) {
      setFile(null);
      setFileKey((current) => current + 1);
      setErrorMessage(getApiErrorMessage(error, "Could not read this CSV."));
    } finally {
      setBusy(null);
    }
  }

  async function previewImport() {
    if (!file || !accountId) {
      setErrorMessage("Choose an account and a CSV file.");
      return;
    }

    const message = mappingError(columns);
    if (message) {
      setErrorMessage(message);
      return;
    }

    setBusy("preview");
    setErrorMessage(null);
    setResult(null);
    try {
      const form = new FormData();
      appendTransactionImport(form, file, accountId, columns);
      const nextPreview = await previewTransactionImport(form);
      setPreview(nextPreview);
      setIncluded(
        new Set(
          nextPreview.rows
            .filter((row) => row.status === "Ready")
            .map((row) => row.lineNumber),
        ),
      );
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Could not preview this CSV."));
    } finally {
      setBusy(null);
    }
  }

  async function importSelected() {
    if (!file || !preview) {
      return;
    }

    const lineNumbers = preview.rows
      .filter((row) => included.has(row.lineNumber))
      .map((row) => row.lineNumber);

    if (lineNumbers.length === 0) {
      setErrorMessage("Choose at least one row to import.");
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

  async function confirmUndo(importId: string) {
    setBusy("undo");
    setErrorMessage(null);
    try {
      await undoTransactionImport(importId);
      setPendingUndoId(null);
      if (result?.id === importId) {
        setResult(null);
      }
      await refreshOpenImports();
      onImported();
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Could not undo this import."));
    } finally {
      setBusy(null);
    }
  }

  function toggleRow(lineNumber: number) {
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

  const headers = inspect?.headers ?? [];
  const requiredColumns = columnChoices(headers, false);
  const optionalColumns = columnChoices(headers, true);
  const includedCount = preview
    ? preview.rows.filter((row) => included.has(row.lineNumber)).length
    : 0;

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-5">
      {openImports.length > 0 ? (
        <section className="flex flex-col gap-2">
          <h3 className="text-sm font-medium">Undo an import</h3>
          {openImports.map((batch) => (
            <div
              key={batch.id}
              className="flex flex-col gap-2 rounded-lg border border-border px-3 py-2"
            >
              <p className="text-sm">
                {batch.fileName} · {batch.importedCount} on {batch.accountName}{" "}
                · {formatBatchDate(batch.createdAt)}
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
                      onClick={() => void confirmUndo(batch.id)}
                    >
                      {busy === "undo"
                        ? "Archiving"
                        : "Archive imported transactions"}
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      disabled={isWorking}
                      onClick={() => setPendingUndoId(null)}
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
                  onClick={() => setPendingUndoId(batch.id)}
                >
                  Undo
                </Button>
              )}
            </div>
          ))}
        </section>
      ) : null}

      {result ? (
        <Alert>
          Imported {result.importedCount} from {result.fileName}. Undo is
          available above until you archive that batch.
        </Alert>
      ) : null}

      {openAccounts.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          Add an account before importing a CSV. You can still add a transaction
          by hand.
        </p>
      ) : (
        <>
          <div className="flex flex-col gap-1.5 text-sm font-medium">
            Account
            <Select
              title="Account"
              value={accountId}
              onChange={(value) => {
                setAccountId(value);
                setPreview(null);
              }}
              onOpenChange={onPickerOpenChange}
              className="h-9"
              options={openAccounts.map((account) => ({
                value: account.id,
                label: account.name,
              }))}
            />
          </div>
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
          <p className="text-xs text-muted-foreground">
            Manual entry stays available. Import adds posted transactions. A
            matching date, amount, and name is left unchecked as a duplicate.
          </p>

          {inspect ? (
            <>
              <p className="text-sm text-muted-foreground">
                {inspect.dataRowCount} transactions in {inspect.fileName}.
              </p>
              {inspect.sampleRows.length > 0 ? (
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
                            <td
                              key={cellIndex}
                              className="px-3 py-2 whitespace-nowrap"
                            >
                              {row[cellIndex] ?? ""}
                            </td>
                          ))}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : null}

              <section className="flex flex-col gap-4 border-t border-border pt-4">
                <h3 className="text-sm font-medium">Columns</h3>
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="flex flex-col gap-1.5 text-sm font-medium">
                    Date
                    <Select
                      title="Date column"
                      value={columns.dateColumn}
                      onChange={(value) =>
                        changeColumns({ ...columns, dateColumn: value })
                      }
                      onOpenChange={onPickerOpenChange}
                      placeholder="Choose a column"
                      className="h-9"
                      options={requiredColumns}
                    />
                  </div>
                  <div className="flex flex-col gap-1.5 text-sm font-medium">
                    Name
                    <Select
                      title="Name column"
                      value={columns.nameColumn}
                      onChange={(value) =>
                        changeColumns({ ...columns, nameColumn: value })
                      }
                      onOpenChange={onPickerOpenChange}
                      placeholder="Choose a column"
                      className="h-9"
                      options={requiredColumns}
                    />
                  </div>
                  <div className="flex flex-col gap-1.5 text-sm font-medium">
                    Amount columns
                    <Select
                      title="Amount columns"
                      value={columns.amountMode}
                      onChange={(value) =>
                        changeColumns({
                          ...columns,
                          amountMode: value === "split" ? "split" : "amount",
                        })
                      }
                      onOpenChange={onPickerOpenChange}
                      className="h-9"
                      options={[
                        { value: "amount", label: "One amount column" },
                        { value: "split", label: "Debit and credit columns" },
                      ]}
                    />
                  </div>
                  <div className="flex flex-col gap-1.5 text-sm font-medium">
                    Date order
                    <Select
                      title="Date order"
                      value={columns.dateOrder}
                      onChange={(value) =>
                        changeColumns({ ...columns, dateOrder: value })
                      }
                      onOpenChange={onPickerOpenChange}
                      className="h-9"
                      options={[
                        { value: "MonthFirst", label: "Month first" },
                        { value: "DayFirst", label: "Day first" },
                      ]}
                    />
                  </div>
                  {columns.amountMode === "amount" ? (
                    <>
                      <div className="flex flex-col gap-1.5 text-sm font-medium">
                        Amount
                        <Select
                          title="Amount column"
                          value={columns.amountColumn}
                          onChange={(value) =>
                            changeColumns({ ...columns, amountColumn: value })
                          }
                          onOpenChange={onPickerOpenChange}
                          placeholder="Choose a column"
                          className="h-9"
                          options={requiredColumns}
                        />
                      </div>
                      <div className="flex flex-col gap-1.5 text-sm font-medium">
                        Positive amounts
                        <Select
                          title="Positive amounts"
                          value={columns.amountSign}
                          onChange={(value) =>
                            changeColumns({ ...columns, amountSign: value })
                          }
                          onOpenChange={onPickerOpenChange}
                          className="h-9"
                          options={[
                            { value: "PositiveOut", label: "Money out" },
                            { value: "PositiveIn", label: "Money in" },
                          ]}
                        />
                      </div>
                    </>
                  ) : (
                    <>
                      <div className="flex flex-col gap-1.5 text-sm font-medium">
                        Debit
                        <Select
                          title="Debit column"
                          value={columns.debitColumn}
                          onChange={(value) =>
                            changeColumns({ ...columns, debitColumn: value })
                          }
                          onOpenChange={onPickerOpenChange}
                          placeholder="Not used"
                          className="h-9"
                          options={optionalColumns}
                        />
                      </div>
                      <div className="flex flex-col gap-1.5 text-sm font-medium">
                        Credit
                        <Select
                          title="Credit column"
                          value={columns.creditColumn}
                          onChange={(value) =>
                            changeColumns({ ...columns, creditColumn: value })
                          }
                          onOpenChange={onPickerOpenChange}
                          placeholder="Not used"
                          className="h-9"
                          options={optionalColumns}
                        />
                      </div>
                    </>
                  )}
                  <div className="flex flex-col gap-1.5 text-sm font-medium">
                    Category
                    <Select
                      title="Category column"
                      value={columns.categoryColumn}
                      onChange={(value) =>
                        changeColumns({ ...columns, categoryColumn: value })
                      }
                      onOpenChange={onPickerOpenChange}
                      placeholder="Not used"
                      className="h-9"
                      options={optionalColumns}
                    />
                  </div>
                  <div className="flex flex-col gap-1.5 text-sm font-medium">
                    Notes
                    <Select
                      title="Notes column"
                      value={columns.notesColumn}
                      onChange={(value) =>
                        changeColumns({ ...columns, notesColumn: value })
                      }
                      onOpenChange={onPickerOpenChange}
                      placeholder="Not used"
                      className="h-9"
                      options={optionalColumns}
                    />
                  </div>
                </div>
                <p className="text-xs text-muted-foreground">
                  Debit is money out and credit is money in. A CR or DR marker
                  sets the direction on its own. Money in counts as income only
                  when the category is Income.
                </p>
                <Button
                  type="button"
                  variant="outline"
                  disabled={isWorking}
                  onClick={() => void previewImport()}
                >
                  {busy === "preview" ? "Working" : "Preview import"}
                </Button>
              </section>
            </>
          ) : null}

          {preview ? (
            <section className="flex flex-col gap-3">
              <p className="text-sm">
                {preview.readyCount} ready · {preview.duplicateCount} duplicates
                · {preview.errorCount} to fix
              </p>
              <ul className="flex flex-col">
                {preview.rows.map((row) => {
                  const disabled = row.status === "Error";
                  return (
                    <li
                      key={row.lineNumber}
                      className="border-b border-border py-3"
                    >
                      <label className="flex gap-3">
                        <input
                          type="checkbox"
                          className="mt-1 size-4 accent-primary"
                          checked={included.has(row.lineNumber)}
                          disabled={disabled || isWorking}
                          aria-label={`Include line ${row.lineNumber} ${row.name ?? ""}`}
                          onChange={() => toggleRow(row.lineNumber)}
                        />
                        <span className="min-w-0 flex-1">
                          <span className="flex flex-wrap items-baseline justify-between gap-2">
                            <span className="font-medium">
                              {row.name || "Untitled"}
                            </span>
                            <span className="text-sm">
                              {formatImportAmount(
                                row.amount,
                                selectedAccount?.isoCurrencyCode ?? null,
                              )}
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
              <Button
                type="button"
                size="lg"
                disabled={isWorking || includedCount === 0}
                onClick={() => void importSelected()}
              >
                {busy === "import"
                  ? "Importing"
                  : `Import ${includedCount} transaction${includedCount === 1 ? "" : "s"}`}
              </Button>
            </section>
          ) : null}
        </>
      )}

      {errorMessage ? (
        <Alert variant="destructive">{errorMessage}</Alert>
      ) : null}
    </div>
  );
}

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
