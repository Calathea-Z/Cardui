"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  archiveAccount,
  reconcileAccountBalance,
  restoreAccount,
  updateManualAccount,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { AccountDto } from "@/lib/api/types";
import { formatCurrency } from "./formatCurrency";
import { isManualAccount, parseMoney, todayDateInput } from "./manualAccount";
import {
  ManualAccountForm,
  type ManualAccountFormValues,
} from "./ManualAccountForm";

type AccountDetailSheetProps = {
  account: AccountDto | null;
  planningCurrency?: string;
  onClose: () => void;
  onChanged: () => void;
};

function toForm(account: AccountDto): ManualAccountFormValues {
  return {
    name: account.name,
    type: account.type,
    subtype: account.subtype ?? "",
    openingBalance: String(account.openingBalance ?? 0),
    openingBalanceDate: account.openingBalanceDate ?? todayDateInput(),
  };
}

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
  const [form, setForm] = useState(() => toForm(account));
  const [statementBalance, setStatementBalance] = useState(
    String(account.currentBalance),
  );
  const [asOfDate, setAsOfDate] = useState(todayDateInput());
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [reconcileMessage, setReconcileMessage] = useState<string | null>(null);
  const [currentBalance, setCurrentBalance] = useState(account.currentBalance);
  const [isSaving, setIsSaving] = useState(false);
  const [confirmArchive, setConfirmArchive] = useState(false);

  async function saveAccount() {
    const openingBalance = parseMoney(form.openingBalance);
    if (
      !form.name.trim() ||
      openingBalance === null ||
      !form.openingBalanceDate
    ) {
      setErrorMessage("Enter a name, opening balance, and opening date.");
      return;
    }

    setIsSaving(true);
    setErrorMessage(null);
    try {
      await updateManualAccount(account.id, {
        name: form.name.trim(),
        type: form.type,
        subtype: form.subtype.trim() || null,
        openingBalance,
        openingBalanceDate: form.openingBalanceDate,
        isoCurrencyCode: account.isoCurrencyCode,
      });
      onChanged();
      onClose();
    } catch (error) {
      setErrorMessage(
        getApiErrorMessage(error, "Could not save this account."),
      );
    } finally {
      setIsSaving(false);
    }
  }

  async function matchStatement() {
    const balance = parseMoney(statementBalance);
    if (balance === null || !asOfDate) {
      setErrorMessage("Enter the statement balance and date.");
      return;
    }

    setIsSaving(true);
    setErrorMessage(null);
    setReconcileMessage(null);
    try {
      const result = await reconcileAccountBalance(account.id, {
        asOfDate,
        statementBalance: balance,
      });
      setCurrentBalance(result.account.currentBalance);
      setReconcileMessage(
        result.adjustment === 0
          ? "The balance already matched this statement."
          : `Saved a ${formatCurrency(result.adjustment, currency)} adjustment. It is not income or spending.`,
      );
      onChanged();
    } catch (error) {
      setErrorMessage(
        getApiErrorMessage(error, "Could not reconcile this balance."),
      );
    } finally {
      setIsSaving(false);
    }
  }

  async function updateArchive() {
    setIsSaving(true);
    setErrorMessage(null);
    try {
      if (account.archivedAt) {
        await restoreAccount(account.id);
      } else {
        await archiveAccount(account.id);
      }
      onChanged();
      onClose();
    } catch (error) {
      setErrorMessage(
        getApiErrorMessage(error, "Could not update this account."),
      );
    } finally {
      setIsSaving(false);
      setConfirmArchive(false);
    }
  }

  return (
    <div className="flex flex-col gap-5">
      <p className="text-sm text-muted-foreground">
        Current balance {formatCurrency(currentBalance, currency)}
      </p>

      {manual ? (
        <ManualAccountForm
          values={form}
          onChange={setForm}
          onSubmit={() => void saveAccount()}
          submitLabel="Save account"
          isSaving={isSaving}
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
              value={statementBalance}
              onChange={(event) => setStatementBalance(event.target.value)}
              inputMode="decimal"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-sm font-medium">
            As of
            <Input
              type="date"
              value={asOfDate}
              onChange={(event) => setAsOfDate(event.target.value)}
            />
          </label>
          <Button
            type="button"
            variant="outline"
            disabled={isSaving}
            onClick={() => void matchStatement()}
          >
            Match statement balance
          </Button>
          {reconcileMessage ? (
            <p className="text-sm text-muted-foreground">{reconcileMessage}</p>
          ) : null}
        </div>
      ) : null}

      {errorMessage ? (
        <Alert variant="destructive">{errorMessage}</Alert>
      ) : null}

      {confirmArchive ? (
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            className="flex-1"
            onClick={() => setConfirmArchive(false)}
          >
            Cancel
          </Button>
          <Button
            type="button"
            variant="destructive"
            className="flex-1"
            disabled={isSaving}
            onClick={() => void updateArchive()}
          >
            Archive
          </Button>
        </div>
      ) : (
        <Button
          type="button"
          variant={account.archivedAt ? "outline" : "destructive"}
          disabled={isSaving}
          onClick={() => {
            if (account.archivedAt) {
              void updateArchive();
              return;
            }
            setConfirmArchive(true);
          }}
        >
          {account.archivedAt ? "Restore account" : "Archive account"}
        </Button>
      )}
    </div>
  );
}

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
      headerAction="back"
      closeOnEscape={!pickerOpen}
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
