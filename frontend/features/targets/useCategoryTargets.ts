"use client";

import { useRef, useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/ui/confirm-dialog";
import {
  clearCategoryTarget,
  copyCategoryTargets,
  getCategoryTargets,
  saveCategoryTarget,
  startFreshCategoryTargets,
} from "@/lib/api/browser";
import { describeApiError } from "@/lib/api/errors";
import type {
  CategoryTargetLineDto,
  CategoryTargetMonthDto,
} from "@/lib/api/types";
import { monthLabel } from "./categoryTargetCopy";
import {
  readTargetAmount,
  targetToForm,
  type CategoryTargetFormState,
} from "./categoryTargetForm";

/**
 * Holds the month of targets and the category form.
 * Switching months reloads that month. A preview is not saved until it is used, edited, or started fresh.
 */
export function useCategoryTargets(
  initialMonth: CategoryTargetMonthDto | null,
) {
  const confirm = useConfirm();
  const [month, setMonth] = useState(initialMonth);
  const [form, setForm] = useState<CategoryTargetFormState>({
    amount: "",
    rollover: false,
  });
  const [editing, setEditing] = useState<CategoryTargetLineDto | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const requestId = useRef(0);

  /**
   * Loads one month and ignores a response that arrived after a newer request.
   */
  async function loadMonth(year: number, monthNumber: number) {
    const id = ++requestId.current;
    setIsUpdating(true);
    try {
      const next = await getCategoryTargets(year, monthNumber);
      if (id !== requestId.current) {
        return;
      }

      setMonth(next);
    } catch (error) {
      if (id !== requestId.current) {
        return;
      }

      reportTargetFailure(error, "Those targets could not be loaded.");
    } finally {
      if (id === requestId.current) {
        setIsUpdating(false);
      }
    }
  }

  /**
   * Opens the form for one spending category.
   */
  function startEditing(line: CategoryTargetLineDto) {
    toast.dismiss(targetToastId);
    setFormError(null);
    setEditing(line);
    setForm(targetToForm(line));
    setIsFormOpen(true);
  }

  /**
   * Closes the form without saving.
   */
  function closeForm() {
    setIsFormOpen(false);
    setEditing(null);
  }

  /**
   * Saves the open category.
   * The first save in a preview month also keeps the other copied targets.
   */
  async function save(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!month || !editing?.categoryId) {
      return false;
    }

    const amount = readTargetAmount(form.amount);
    if (!amount.ok) {
      setFormError(amount.error);
      return false;
    }

    setFormError(null);

    setIsSaving(true);
    try {
      const next = await saveCategoryTarget(editing.categoryId, {
        year: month.year,
        month: month.month,
        amount: amount.amount,
        rollover: form.rollover,
      });
      setMonth(next);
      toast.success(`Saved the ${editing.name} target.`, { id: targetToastId });
      return true;
    } catch (error) {
      reportTargetFailure(error, "That target could not be saved.");
      return false;
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Removes the open category's target after confirmation.
   * Spending stays. The month stays started.
   */
  async function clear() {
    if (!month || !editing?.categoryId || editing.target === null) {
      return;
    }

    const confirmed = await confirm({
      title: `Remove the ${editing.name} target?`,
      description: month.saved
        ? "Spending stays. Rollover stops for this category."
        : "Spending stays. The other categories still start from last month.",
      confirmLabel: "Remove",
    });
    if (!confirmed) {
      return;
    }

    setIsSaving(true);
    try {
      const next = await clearCategoryTarget(
        editing.categoryId,
        month.year,
        month.month,
      );
      setMonth(next);
      toast.success(`Removed the ${editing.name} target.`, {
        id: targetToastId,
      });
      closeForm();
    } catch (error) {
      reportTargetFailure(error, "That target could not be removed.");
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Stores the preview as this month's targets.
   */
  async function useCopiedTargets() {
    if (!month || month.saved) {
      return;
    }

    setIsUpdating(true);
    try {
      const next = await copyCategoryTargets(month.year, month.month);
      setMonth(next);
      const from =
        month.copiedFromYear !== null && month.copiedFromMonth !== null
          ? monthLabel(month.copiedFromYear, month.copiedFromMonth)
          : "last month";
      toast.success(
        `${monthLabel(month.year, month.month)} now uses ${from}.`,
        {
          id: targetToastId,
        },
      );
    } catch (error) {
      reportTargetFailure(error, "Those targets could not be saved.");
    } finally {
      setIsUpdating(false);
    }
  }

  /**
   * Starts the month with no targets after confirmation.
   */
  async function startFresh() {
    if (!month || month.saved) {
      return;
    }

    const label = monthLabel(month.year, month.month);
    const confirmed = await confirm({
      title: `Start ${label} with no targets?`,
      description: "Last month's amounts will not be copied.",
      confirmLabel: "Start fresh",
    });
    if (!confirmed) {
      return;
    }

    setIsUpdating(true);
    try {
      const next = await startFreshCategoryTargets(month.year, month.month);
      setMonth(next);
      toast.success(`${label} starts with no targets.`, { id: targetToastId });
    } catch (error) {
      reportTargetFailure(error, "That month could not be started.");
    } finally {
      setIsUpdating(false);
    }
  }

  return {
    month,
    form,
    setForm,
    editing,
    isFormOpen,
    isSaving,
    isUpdating,
    loadMonth,
    startEditing,
    closeForm,
    save,
    clear,
    useCopiedTargets,
    startFresh,
    formError,
  };
}

const targetToastId = "target-action";

/**
 * Shows a target failure as a toast.
 * A 500 uses the sentence about the action. Validation text stays as the toast message.
 */
function reportTargetFailure(error: unknown, action: string) {
  const text = describeApiError(error, action);
  toast.error(text.message, {
    id: targetToastId,
    ...(text.detail ? { description: text.detail } : {}),
  });
}
