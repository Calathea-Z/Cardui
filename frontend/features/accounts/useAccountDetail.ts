"use client";

import { useState } from "react";
import {
  formatCurrency,
  moneyCommaError,
} from "@/features/accounts/formatCurrency";
import {
  archiveAccount,
  reconcileAccountBalance,
  restoreAccount,
  updateManualAccount,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { AccountDto } from "@/lib/api/types";
import {
  parseMoney,
  todayDateInput,
  toManualAccountType,
} from "./manualAccount";
import type { ManualAccountFormValues } from "./ManualAccountForm";

type UseAccountDetailOptions = {
  account: AccountDto;
  currency: string;
  onClose: () => void;
  onChanged: () => void;
};

/**
 * Copies an account into the manual edit form.
 * A missing opening date is filled with today's local date.
 */
function toForm(account: AccountDto): ManualAccountFormValues {
  return {
    name: account.name,
    type: toManualAccountType(account.type),
    subtype: account.subtype ?? "",
    openingBalance: String(account.openingBalance ?? 0),
    openingBalanceDate: account.openingBalanceDate ?? todayDateInput(),
  };
}

/**
 * Edits, reconciles, archives, or restores one account.
 * A manual save or an archive closes the sheet. A statement match stays open and shows the new balance.
 */
export function useAccountDetail({
  account,
  currency,
  onClose,
  onChanged,
}: UseAccountDetailOptions) {
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

  /**
   * Saves the manual account name, type, subtype, and opening balance.
   * A blank name, invalid opening balance, or missing opening date shows an error and skips the request.
   */
  async function saveAccount() {
    const openingBalance = parseMoney(form.openingBalance);
    const openingComma = moneyCommaError(form.openingBalance);
    if (openingComma) {
      setErrorMessage(openingComma);
      return;
    }

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

  /**
   * Reconciles a manual account to a statement balance on a chosen date.
   * A blank balance or date stops the request, and a zero result means the balance already matched.
   */
  async function matchStatement() {
    const balance = parseMoney(statementBalance);
    const statementComma = moneyCommaError(statementBalance);
    if (statementComma) {
      setErrorMessage(statementComma);
      return;
    }

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

  /**
   * Archives an open account, or restores one that is already archived.
   * The sheet closes after the account updates.
   */
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

  /**
   * Restores an archived account immediately, or asks before archiving an open one.
   */
  function beginArchive() {
    if (account.archivedAt) {
      void updateArchive();
      return;
    }

    setConfirmArchive(true);
  }

  return {
    form,
    setForm,
    statementBalance,
    setStatementBalance,
    asOfDate,
    setAsOfDate,
    errorMessage,
    reconcileMessage,
    currentBalance,
    isSaving,
    confirmArchive,
    saveAccount,
    matchStatement,
    updateArchive,
    beginArchive,
    cancelArchive: () => setConfirmArchive(false),
  };
}
