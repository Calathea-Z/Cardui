"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { DateField } from "@/components/ui/date-field";
import { Input } from "@/components/ui/input";
import type { AccountDto } from "@/lib/api/types";
import { formatCurrency } from "./formatCurrency";
import { isManualAccount } from "./manualAccount";
import { ManualAccountForm } from "./ManualAccountForm";
import { useAccountDetail } from "./useAccountDetail";

type AccountDetailSheetProps = {
  account: AccountDto | null;
  planningCurrency?: string;
  onClose: () => void;
  onChanged: () => void;
};

/**
 * Shows one account's current balance and the actions available for it.
 * A manual account can be edited, and an active manual account can be matched to a statement. Archive asks for confirmation. Restore applies immediately.
 */
function AccountDetailContent({
  account,
  planningCurrency = "USD",
  onClose,
  onChanged,
  onPickerOpenChange,
}: {
  account: AccountDto;
  planningCurrency?: string;
  onClose: () => void;
  onChanged: () => void;
  onPickerOpenChange: (open: boolean) => void;
}) {
  const currency = account.isoCurrencyCode ?? planningCurrency;
  const manual = isManualAccount(account);
  const detail = useAccountDetail({ account, currency, onClose, onChanged });

  return (
    <div className="flex flex-col gap-5">
      <p className="text-sm text-muted-foreground">
        Current balance {formatCurrency(detail.currentBalance, currency)}
      </p>

      {manual ? (
        <ManualAccountForm
          values={detail.form}
          onChange={detail.setForm}
          onSubmit={() => void detail.saveAccount()}
          submitLabel="Save account"
          isSaving={detail.isSaving}
          errorMessage={null}
          onPickerOpenChange={onPickerOpenChange}
        />
      ) : (
        <p className="text-sm text-muted-foreground">
          This account is linked. Its name and balance come from the bank. You
          can archive it without removing the connection.
        </p>
      )}

      {manual && !account.archivedAt ? (
        <div className="flex flex-col gap-3 border-t border-border/70 pt-4">
          <h3 className="text-sm font-medium">Match a statement</h3>
          <p className="text-xs text-muted-foreground">
            If the statement differs from the calculated balance, an adjustment
            is saved. That adjustment is not income or spending.
          </p>
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            Statement balance
            <Input
              value={detail.statementBalance}
              onChange={(event) =>
                detail.setStatementBalance(event.target.value)
              }
              inputMode="decimal"
            />
          </label>
          <div className="flex flex-col gap-1.5 text-sm font-medium">
            As of
            <DateField
              title="As of"
              value={detail.asOfDate}
              onChange={detail.setAsOfDate}
              onOpenChange={onPickerOpenChange}
              className="h-9"
            />
          </div>
          <Button
            type="button"
            variant="outline"
            disabled={detail.isSaving}
            onClick={() => void detail.matchStatement()}
          >
            Match statement balance
          </Button>
          {detail.reconcileMessage ? (
            <p className="text-sm text-muted-foreground">
              {detail.reconcileMessage}
            </p>
          ) : null}
        </div>
      ) : null}

      {detail.errorMessage ? (
        <Alert variant="destructive">{detail.errorMessage}</Alert>
      ) : null}

      {detail.confirmArchive ? (
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            className="flex-1"
            onClick={detail.cancelArchive}
          >
            Cancel
          </Button>
          <Button
            type="button"
            variant="destructive"
            className="flex-1"
            disabled={detail.isSaving}
            onClick={() => void detail.updateArchive()}
          >
            Archive
          </Button>
        </div>
      ) : (
        <Button
          type="button"
          variant={account.archivedAt ? "outline" : "destructive"}
          disabled={detail.isSaving}
          onClick={detail.beginArchive}
        >
          {account.archivedAt ? "Restore account" : "Archive account"}
        </Button>
      )}
    </div>
  );
}

/**
 * Opens the detail sheet for the selected account.
 * Escape closes the sheet while a choice list or the date calendar is closed, and a different account remounts the form.
 */
export function AccountDetailSheet({
  account,
  planningCurrency = "USD",
  onClose,
  onChanged,
}: AccountDetailSheetProps) {
  const [pickerOpen, setPickerOpen] = useState(false);

  return (
    <BottomSheet
      open={account !== null}
      onClose={onClose}
      title={account?.name ?? "Account"}
      headerAction="panel"
      closeOnEscape={!pickerOpen}
      presentation="panel"
    >
      {account ? (
        <AccountDetailContent
          key={account.id}
          account={account}
          planningCurrency={planningCurrency}
          onClose={onClose}
          onChanged={onChanged}
          onPickerOpenChange={setPickerOpen}
        />
      ) : null}
    </BottomSheet>
  );
}
