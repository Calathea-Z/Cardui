"use client";

import { useEffect, useRef, useState } from "react";
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
import {
  appendTransactionImport,
  columnStateFromSuggestion,
  emptyImportColumns,
  mappingError,
  maxCsvBytes,
  nextImportStep,
  previousImportStep,
  type ImportBusy,
  type ImportColumnState,
  type ImportStep,
} from "./importCsv";

type UseImportCsvOptions = {
  accounts: AccountDto[];
  onImported: () => void;
};

/**
 * Walks the household through choosing a file, mapping columns, choosing rows, and importing.
 * Back keeps the account, file, mapping, and row choices, and the file must be a .csv of at most 1 MB.
 */
export function useImportCsv({ accounts, onImported }: UseImportCsvOptions) {
  const openAccounts = accounts.filter((account) => !account.archivedAt);
  const [step, setStep] = useState<ImportStep>("account");
  const [accountId, setAccountId] = useState(openAccounts[0]?.id ?? "");
  const [file, setFile] = useState<File | null>(null);
  const [fileKey, setFileKey] = useState(0);
  const [inspect, setInspect] = useState<TransactionImportInspectDto | null>(
    null,
  );
  const [columns, setColumns] = useState<ImportColumnState>(emptyImportColumns);
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
  const leftOut = preview ? preview.rows.length - includedCount : 0;

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
   * Stores a chosen account and clears a preview that belonged to the previous one.
   */
  function changeAccount(value: string) {
    setAccountId(value);
    setPreview(null);
    setIncluded(new Set());
    setErrorMessage(null);
  }

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
    setColumns(emptyImportColumns);
    setErrorMessage(null);
    setPendingUndoId(null);
    setStep("account");
  }

  return {
    openAccounts,
    step,
    accountId,
    changeAccount,
    fileKey,
    inspect,
    columns,
    changeColumns,
    preview,
    included,
    openImports,
    pendingUndoId,
    setPendingUndoId,
    result,
    errorMessage,
    busy,
    isWorking,
    selectedAccount,
    includedCount,
    includedDuplicates,
    undoBatches,
    leftOut,
    onFileSelected,
    continueFromAccount,
    previewAndContinue,
    continueFromRows,
    goBack,
    importSelected,
    confirmUndo,
    toggleRow,
    startAnother,
  };
}
