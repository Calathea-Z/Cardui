"use client";

import { useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/ui/confirm-dialog";
import {
  createSavingsGoal,
  deleteSavingsGoal,
  updateSavingsGoal,
} from "@/lib/api/browser";
import { describeApiError } from "@/lib/api/errors";
import type {
  SavingsAccountDto,
  SavingsGoalDto,
  SavingsGoalKind,
} from "@/lib/api/types";
import {
  emptySavingsForm,
  goalToForm,
  toSavingsUpsert,
  type SavingsFormState,
} from "./savingsFormState";

/**
 * Holds the savings list and the create or edit form.
 * Saving a goal does not move money. The form opens over the list.
 */
export function useSavingsGoals(
  initialGoals: SavingsGoalDto[],
  accounts: SavingsAccountDto[],
) {
  const [goals, setGoals] = useState(initialGoals);
  const [form, setForm] = useState<SavingsFormState>(
    emptySavingsForm("Sinking"),
  );
  const [editingId, setEditingId] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const confirm = useConfirm();

  /**
   * Creates a goal or saves the one being edited.
   * Returns true when it was saved. The target and date are required.
   */
  async function handleSubmit(
    event: React.FormEvent<HTMLFormElement>,
  ): Promise<boolean> {
    event.preventDefault();
    const offered =
      !form.accountId ||
      accounts.some(
        (account) =>
          account.id === form.accountId &&
          (account.followedByGoalId === null ||
            account.followedByGoalId === editingId),
      );
    const payload = toSavingsUpsert(form, offered);
    if (!payload.ok) {
      showSavingsError(payload.error);
      return false;
    }

    toast.dismiss(savingsToastId);
    setIsSaving(true);
    try {
      if (editingId) {
        const updated = await updateSavingsGoal(editingId, payload.dto);
        setGoals((current) =>
          current.map((goal) => (goal.id === updated.id ? updated : goal)),
        );
      } else {
        const created = await createSavingsGoal(payload.dto);
        setGoals((current) => [...current, created]);
      }

      return true;
    } catch (err) {
      reportSavingsFailure(err, "That goal could not be saved.");
      return false;
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Opens a blank form for one kind of goal.
   */
  function startAdding(kind: SavingsGoalKind) {
    toast.dismiss(savingsToastId);
    setEditingId(null);
    setForm(emptySavingsForm(kind));
    setIsFormOpen(true);
  }

  /**
   * Opens the form with one saved goal.
   */
  function startEditing(goal: SavingsGoalDto) {
    toast.dismiss(savingsToastId);
    setEditingId(goal.id);
    setForm(goalToForm(goal));
    setIsFormOpen(true);
  }

  /**
   * Closes the form.
   * The next open replaces whatever was left in the fields.
   */
  function closeForm() {
    toast.dismiss(savingsToastId);
    setIsFormOpen(false);
  }

  /**
   * Deletes a goal after the user confirms.
   * The account and its balance stay.
   */
  async function remove(goal: SavingsGoalDto) {
    const confirmed = await confirm({
      title: `Remove ${goal.name}?`,
      description:
        "This deletes the goal. The account and its balance stay. No transaction is created.",
      confirmLabel: "Remove",
    });
    if (!confirmed) {
      return;
    }

    setBusyId(goal.id);
    try {
      await deleteSavingsGoal(goal.id);
      setGoals((current) => current.filter((item) => item.id !== goal.id));
      if (editingId === goal.id) {
        closeForm();
      }
    } catch (err) {
      reportSavingsFailure(err, "That goal could not be removed.");
    } finally {
      setBusyId(null);
    }
  }

  return {
    form,
    setForm,
    editingId,
    isFormOpen,
    goals,
    isSaving,
    busyId,
    handleSubmit,
    startAdding,
    startEditing,
    closeForm,
    remove,
  };
}

const savingsToastId = "savings-action";

/**
 * Shows a savings failure as a toast.
 * A 500 uses the sentence about the action. Validation text stays as the toast message.
 */
function reportSavingsFailure(error: unknown, action: string) {
  const text = describeApiError(error, action);
  showSavingsError(text.message, text.detail);
}

/**
 * Shows one savings error toast.
 * A later savings action replaces this toast.
 */
function showSavingsError(message: string, detail?: string) {
  toast.error(message, {
    id: savingsToastId,
    ...(detail ? { description: detail } : {}),
  });
}
