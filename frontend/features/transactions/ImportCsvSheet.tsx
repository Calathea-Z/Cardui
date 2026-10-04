"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { cn } from "@/lib/utils";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import type { AccountDto } from "@/lib/api/types";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";
import { ImportColumnMapping } from "./ImportColumnMapping";
import { ImportLabeledSelect } from "./ImportLabeledSelect";
import { ImportPreviewRows } from "./ImportPreviewRows";
import { ImportSampleRows } from "./ImportSampleRows";
import { ImportStepIndicator } from "./ImportStepIndicator";
import { transactionLabel } from "./importCsvFormat";
import { OpenImportList } from "./OpenImportList";
import { useImportCsv } from "./useImportCsv";

type ImportCsvSheetProps = {
  open: boolean;
  accounts: AccountDto[];
  onClose: () => void;
  onImported: () => void;
};

type ImportCsvFormProps = {
  accounts: AccountDto[];
  onImported: () => void;
  onPickerOpenChange: (open: boolean) => void;
};

/**
 * Walks through the import steps.
 * The account, file, mapping, and row choices stay when the household goes back.
 */
function ImportCsvForm({
  accounts,
  onImported,
  onPickerOpenChange,
}: ImportCsvFormProps) {
  const csv = useImportCsv({ accounts, onImported });

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-5">
      <ImportStepIndicator step={csv.result ? "import" : csv.step} />

      {csv.step === "account" && csv.openAccounts.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          Add an account before importing a CSV. You can still add a transaction
          by hand.
        </p>
      ) : null}

      {csv.step === "account" && csv.openAccounts.length > 0 ? (
        <>
          <ImportLabeledSelect
            label="Account"
            title="Account"
            value={csv.accountId}
            onChange={csv.changeAccount}
            onOpenChange={onPickerOpenChange}
            options={csv.openAccounts.map((account) => ({
              value: account.id,
              label: account.name,
            }))}
          />
          {csv.selectedAccount?.plaidItemId ? (
            <p className="text-xs text-muted-foreground">
              This account is linked. The bank still supplies its balance. The
              imported transactions count in activity.
            </p>
          ) : null}
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            CSV file
            <input
              key={csv.fileKey}
              type="file"
              accept=".csv,text/csv"
              className="text-sm file:mr-3 file:rounded-lg file:border-0 file:bg-secondary file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-secondary-foreground"
              onChange={(event) => {
                void csv.onFileSelected(event.target.files?.[0] ?? null);
              }}
            />
          </label>
          {csv.busy === "inspect" ? (
            <p className="text-sm text-muted-foreground">Reading the file.</p>
          ) : null}
          {csv.inspect ? (
            <p className="text-sm text-muted-foreground">
              {csv.inspect.dataRowCount} transactions in {csv.inspect.fileName}.
            </p>
          ) : null}
          <p className="text-xs text-muted-foreground">
            Manual entry stays available. Import adds posted transactions.
          </p>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              disabled={csv.isWorking}
              onClick={csv.continueFromAccount}
            >
              Continue
            </Button>
          </div>
        </>
      ) : null}

      {csv.step === "columns" && csv.inspect ? (
        <>
          <p className="text-sm text-muted-foreground">
            This says which column becomes the date, the name, and the amount.
            The column names from the file are already selected. Change one when
            it points at the wrong column.
          </p>
          <p className="text-sm text-muted-foreground">
            {csv.inspect.dataRowCount} transactions in {csv.inspect.fileName}.
          </p>
          <ImportSampleRows inspect={csv.inspect} />
          <ImportColumnMapping
            headers={csv.inspect.headers}
            columns={csv.columns}
            onChange={csv.changeColumns}
            onOpenChange={onPickerOpenChange}
          />
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={csv.isWorking}
              onClick={csv.goBack}
            >
              Back
            </Button>
            <Button
              type="button"
              disabled={csv.isWorking}
              onClick={() => void csv.previewAndContinue()}
            >
              {csv.busy === "preview"
                ? "Working"
                : csv.preview
                  ? "Continue"
                  : "Preview"}
            </Button>
          </div>
        </>
      ) : null}

      {csv.step === "rows" && csv.preview ? (
        <>
          <p className="text-sm">
            {csv.preview.readyCount} ready · {csv.preview.duplicateCount}{" "}
            duplicates · {csv.preview.errorCount} to fix
          </p>
          <p className="text-xs text-muted-foreground">
            Ready rows are checked. A duplicate stays unchecked until you
            include it. A row that needs a fix cannot be imported.
          </p>
          <ImportPreviewRows
            rows={csv.preview.rows}
            included={csv.included}
            currency={csv.selectedAccount?.isoCurrencyCode ?? null}
            isWorking={csv.isWorking}
            onToggle={csv.toggleRow}
          />
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={csv.isWorking}
              onClick={csv.goBack}
            >
              Back
            </Button>
            <Button
              type="button"
              disabled={csv.isWorking || csv.includedCount === 0}
              onClick={csv.continueFromRows}
            >
              Continue
            </Button>
          </div>
        </>
      ) : null}

      {csv.step === "import" && csv.preview && !csv.result ? (
        <>
          <p className="text-sm">
            Import {transactionLabel(csv.includedCount)} onto{" "}
            {csv.selectedAccount?.name ?? "this account"} from{" "}
            {csv.inspect?.fileName ?? "this file"}.
          </p>
          <p className="text-sm text-muted-foreground">
            {csv.includedCount - csv.includedDuplicates} ready
            {csv.includedDuplicates > 0
              ? ` · ${csv.includedDuplicates} duplicate${csv.includedDuplicates === 1 ? "" : "s"} included`
              : ""}
            {csv.leftOut > 0 ? ` · ${csv.leftOut} not included` : ""}
          </p>
          {csv.selectedAccount?.plaidItemId ? (
            <p className="text-xs text-muted-foreground">
              This account is linked. The bank still supplies its balance. The
              imported transactions count in activity.
            </p>
          ) : null}
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={csv.isWorking}
              onClick={csv.goBack}
            >
              Back
            </Button>
            <Button
              type="button"
              size="lg"
              disabled={csv.isWorking || csv.includedCount === 0}
              onClick={() => void csv.importSelected()}
            >
              {csv.busy === "import"
                ? "Importing"
                : `Import ${transactionLabel(csv.includedCount)}`}
            </Button>
          </div>
        </>
      ) : null}

      {csv.result ? (
        <>
          <Alert>
            Imported {csv.result.importedCount} from {csv.result.fileName}. Undo
            is below until you archive that batch.
          </Alert>
          <OpenImportList
            batches={csv.undoBatches}
            pendingUndoId={csv.pendingUndoId}
            busy={csv.busy}
            onAsk={csv.setPendingUndoId}
            onCancel={() => csv.setPendingUndoId(null)}
            onConfirm={(importId) => void csv.confirmUndo(importId)}
          />
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={csv.isWorking}
              onClick={csv.startAnother}
            >
              Import another file
            </Button>
          </div>
        </>
      ) : null}

      {csv.errorMessage ? (
        <Alert variant="destructive">{csv.errorMessage}</Alert>
      ) : null}

      {csv.step === "account" && !csv.result ? (
        <OpenImportList
          batches={csv.openImports}
          pendingUndoId={csv.pendingUndoId}
          busy={csv.busy}
          onAsk={csv.setPendingUndoId}
          onCancel={() => csv.setPendingUndoId(null)}
          onConfirm={(importId) => void csv.confirmUndo(importId)}
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
