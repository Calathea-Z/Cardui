"use client";

import { useRef, useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/ui/confirm-dialog";
import {
  chooseAccountBalance,
  createDebt,
  deleteDebt,
  getDebtSummary,
  updateDebt,
} from "@/lib/api/browser";
import { describeApiError } from "@/lib/api/errors";
import type { DebtDto, DebtSummaryReportDto } from "@/lib/api/types";
import {
  debtToForm,
  emptyDebtForm,
  toDebtUpsert,
  type DebtFormState,
} from "./debtFormState";

/**
 * Holds the debt list, its summary, and the create or edit form.
 * A blank term stays unknown. Summary keeps the recorded balance until the person chooses the account balance.
 */
export function useDebts(
  initialDebts: DebtDto[],
  initialSummary: DebtSummaryReportDto | null,
) {
  const [debts, setDebts] = useState(initialDebts);
  const [summary, setSummary] = useState(initialSummary);
  const [summaryUpdating, setSummaryUpdating] = useState(false);
  const [form, setForm] = useState<DebtFormState>(emptyDebtForm());
  const [editingId, setEditingId] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const summaryRequest = useRef(0);
  const confirm = useConfirm();

  /**
   * Creates a debt or saves the one being edited.
   * Returns true when the debt was saved. The name and type are required. A blank term stays unknown.
   */
  async function handleSubmit(
    event: React.FormEvent<HTMLFormElement>,
  ): Promise<boolean> {
    event.preventDefault();
    const payload = toDebtUpsert(form);
    if (!payload.ok) {
      showDebtError(payload.error);
      return false;
    }

    toast.dismiss(debtToastId);
    setIsSaving(true);

    try {
      if (editingId) {
        const updated = await updateDebt(editingId, payload.dto);
        setDebts((current) =>
          current.map((debt) => (debt.id === updated.id ? updated : debt)),
        );
      } else {
        const created = await createDebt(payload.dto);
        setDebts((current) => [...current, created]);
      }

      void refreshSummary();
      return true;
    } catch (err) {
      reportDebtFailure(err, "That debt could not be saved.");
      return false;
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Opens a blank form for a new debt.
   */
  function startAdding() {
    toast.dismiss(debtToastId);
    setEditingId(null);
    setForm(emptyDebtForm());
    setIsFormOpen(true);
  }

  /**
   * Opens the form with one saved debt.
   */
  function startEditing(debt: DebtDto) {
    toast.dismiss(debtToastId);
    setEditingId(debt.id);
    setForm(debtToForm(debt));
    setIsFormOpen(true);
  }

  /**
   * Closes the form.
   * The next open replaces whatever was left in the fields.
   */
  function closeForm() {
    toast.dismiss(debtToastId);
    setIsFormOpen(false);
  }

  /**
   * Deletes a debt after the user confirms.
   * The linked account and its balance stay.
   */
  async function remove(debt: DebtDto) {
    const confirmed = await confirm({
      title: `Remove ${debt.name}?`,
      description:
        "This deletes the debt. The linked account and its balance stay.",
      confirmLabel: "Remove",
    });
    if (!confirmed) {
      return;
    }

    setBusyId(debt.id);
    try {
      await deleteDebt(debt.id);
      setDebts((current) => current.filter((item) => item.id !== debt.id));
      if (editingId === debt.id) {
        closeForm();
      }

      void refreshSummary();
    } catch (err) {
      reportDebtFailure(err, "That debt could not be removed.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Stores the linked account's dated balance on the debt after confirmation.
   * The account balance is not changed. APR, minimum, and due date stay as they are.
   */
  async function chooseBalance(debt: DebtDto) {
    const comparison = summary?.debts.find(
      (item) => item.debtId === debt.id,
    )?.balanceComparison;
    if (!comparison?.canUseAccountBalance) {
      return;
    }

    const confirmed = await confirm({
      title: `Use the account balance for ${debt.name}?`,
      description:
        debt.balance === null
          ? "The debt balance stays unknown until you do. This saves the account balance and its date on the debt. The account itself is not changed."
          : "The plan keeps the recorded balance until you do. This saves the account balance and its date on the debt. The account itself is not changed.",
      confirmLabel: "Use the account balance",
    });
    if (!confirmed) {
      return;
    }

    setBusyId(debt.id);
    try {
      const updated = await chooseAccountBalance(debt.id);
      setDebts((current) =>
        current.map((item) => (item.id === updated.id ? updated : item)),
      );
      const refreshed = await refreshSummary();
      if (refreshed) {
        toast.success("The recorded balance now matches the account.", {
          id: debtToastId,
        });
      }
    } catch (err) {
      reportDebtFailure(err, "That account balance could not be saved.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Reloads summary after a debt change.
   * The previous summary stays visible until the new one arrives. A failed reload leaves it in place.
   */
  async function refreshSummary() {
    const request = summaryRequest.current + 1;
    summaryRequest.current = request;
    setSummaryUpdating(true);
    try {
      const next = await getDebtSummary();
      if (summaryRequest.current === request) {
        setSummary(next);
      }

      return summaryRequest.current === request;
    } catch (err) {
      if (summaryRequest.current === request) {
        reportDebtFailure(err, "Debt summary could not be updated.");
      }

      return false;
    } finally {
      if (summaryRequest.current === request) {
        setSummaryUpdating(false);
      }
    }
  }

  return {
    form,
    setForm,
    editingId,
    isFormOpen,
    debts: sortDebts(debts),
    summary,
    summaryUpdating,
    isSaving,
    busyId,
    handleSubmit,
    startAdding,
    startEditing,
    closeForm,
    remove,
    chooseBalance,
  };
}

const debtToastId = "debt-action";

/**
 * Shows a debt failure as a toast.
 * A 500 uses the sentence about the action, and in development the exception is the second line.
 * Validation text stays as the toast message.
 */
function reportDebtFailure(error: unknown, action: string) {
  const text = describeApiError(error, action);
  showDebtError(text.message, text.detail);
}

/**
 * Shows one debt error toast.
 * A later debt action replaces this toast.
 */
function showDebtError(message: string, detail?: string) {
  toast.error(message, {
    id: debtToastId,
    ...(detail ? { description: detail } : {}),
  });
}

/**
 * Orders debts by name.
 * The page uses this after a save so a new debt does not stay at the bottom.
 */
function sortDebts(debts: DebtDto[]) {
  return [...debts].sort((left, right) => left.name.localeCompare(right.name));
}
