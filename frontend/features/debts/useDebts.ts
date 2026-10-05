"use client";

import { useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { createDebt, deleteDebt, updateDebt } from "@/lib/api/browser";
import { describeApiError } from "@/lib/api/errors";
import type { DebtDto } from "@/lib/api/types";
import {
  debtToForm,
  emptyDebtForm,
  toDebtUpsert,
  type DebtFormState,
} from "./debtFormState";

/**
 * Holds the debt list and the create or edit form.
 * A blank term stays unknown. The form opens over the list.
 */
export function useDebts(initialDebts: DebtDto[]) {
  const [debts, setDebts] = useState(initialDebts);
  const [form, setForm] = useState<DebtFormState>(emptyDebtForm());
  const [editingId, setEditingId] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
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
    } catch (err) {
      reportDebtFailure(err, "That debt could not be removed.");
    } finally {
      setBusyId(null);
    }
  }

  return {
    form,
    setForm,
    editingId,
    isFormOpen,
    debts: sortDebts(debts),
    isSaving,
    busyId,
    handleSubmit,
    startAdding,
    startEditing,
    closeForm,
    remove,
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
