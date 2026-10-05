"use client";

import { useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/ui/confirm-dialog";
import {
  createObligation,
  deleteObligation,
  dismissObligationSuggestion,
  updateObligation,
} from "@/lib/api/browser";
import { describeApiError } from "@/lib/api/errors";
import type { ObligationDto, ObligationSuggestionDto } from "@/lib/api/types";
import {
  emptyBillForm,
  obligationToForm,
  suggestionToForm,
  toObligationUpsert,
  type BillFormState,
} from "./billFormState";
import { suggestionIsCovered } from "./suggestionMatch";

/**
 * Holds the bill list and the create or edit form.
 * The amount stays one payment. The form opens over the list.
 * Suggestions stay off the bill list until one is added or left out.
 */
export function useBills(
  initialObligations: ObligationDto[],
  initialSuggestions: ObligationSuggestionDto[],
) {
  const [obligations, setObligations] = useState(initialObligations);
  const [suggestions, setSuggestions] = useState(initialSuggestions);
  const [form, setForm] = useState<BillFormState>(emptyBillForm());
  const [editingId, setEditingId] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [busySuggestionKey, setBusySuggestionKey] = useState<string | null>(
    null,
  );
  const confirm = useConfirm();

  /**
   * Creates a bill or saves the one being edited.
   * Returns true when the bill was saved. Name, amount, cadence, date, and flexibility are required.
   */
  async function handleSubmit(
    event: React.FormEvent<HTMLFormElement>,
  ): Promise<boolean> {
    event.preventDefault();
    const payload = toObligationUpsert(form);
    if (!payload.ok) {
      showBillError(payload.error);
      return false;
    }

    toast.dismiss(billToastId);
    setIsSaving(true);

    try {
      if (editingId) {
        const updated = await updateObligation(editingId, payload.dto);
        setObligations((current) =>
          current.map((obligation) =>
            obligation.id === updated.id ? updated : obligation,
          ),
        );
      } else {
        const created = await createObligation(payload.dto);
        setObligations((current) => [...current, created]);
        setSuggestions((current) =>
          current.filter(
            (suggestion) =>
              !suggestionIsCovered(
                suggestion,
                created.name,
                payload.dto.suggestionKey,
              ),
          ),
        );
      }

      return true;
    } catch (err) {
      reportBillFailure(err, "That bill could not be saved.");
      return false;
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Opens a blank form for a new bill.
   */
  function startAdding() {
    toast.dismiss(billToastId);
    setEditingId(null);
    setForm(emptyBillForm());
    setIsFormOpen(true);
  }

  /**
   * Opens the form from a suggestion.
   * Essential or flexible stays unset. The suggestion is not a bill until it is saved.
   */
  function startFromSuggestion(suggestion: ObligationSuggestionDto) {
    toast.dismiss(billToastId);
    setEditingId(null);
    setForm(suggestionToForm(suggestion));
    setIsFormOpen(true);
  }

  /**
   * Opens the form with one saved bill.
   */
  function startEditing(obligation: ObligationDto) {
    toast.dismiss(billToastId);
    setEditingId(obligation.id);
    setForm(obligationToForm(obligation));
    setIsFormOpen(true);
  }

  /**
   * Closes the form.
   * The next open replaces whatever was left in the fields.
   */
  function closeForm() {
    toast.dismiss(billToastId);
    setIsFormOpen(false);
  }

  /**
   * Deletes a bill after the user confirms.
   * The source account and its balance stay.
   */
  async function remove(obligation: ObligationDto) {
    const confirmed = await confirm({
      title: `Remove ${obligation.name}?`,
      description: "This deletes the bill. The account and its balance stay.",
      confirmLabel: "Remove",
    });
    if (!confirmed) {
      return;
    }

    setBusyId(obligation.id);
    try {
      await deleteObligation(obligation.id);
      setObligations((current) =>
        current.filter((item) => item.id !== obligation.id),
      );
      if (editingId === obligation.id) {
        closeForm();
      }
    } catch (err) {
      reportBillFailure(err, "That bill could not be removed.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Leaves a suggestion out of bills after the user confirms.
   * The pattern is not suggested again, and no bill is created.
   */
  async function dismissSuggestion(suggestion: ObligationSuggestionDto) {
    const confirmed = await confirm({
      title: `Leave ${suggestion.name} out of bills?`,
      description:
        "It will not be added, and this pattern will not be suggested again.",
      confirmLabel: "Not a bill",
    });
    if (!confirmed) {
      return;
    }

    setBusySuggestionKey(suggestion.key);
    try {
      await dismissObligationSuggestion(suggestion.key);
      setSuggestions((current) =>
        current.filter((item) => item.key !== suggestion.key),
      );
    } catch (err) {
      reportBillFailure(err, "That suggestion could not be left out.");
    } finally {
      setBusySuggestionKey(null);
    }
  }

  return {
    form,
    setForm,
    editingId,
    isFormOpen,
    obligations: sortObligations(obligations),
    suggestions,
    isSaving,
    busyId,
    busySuggestionKey,
    handleSubmit,
    startAdding,
    startFromSuggestion,
    startEditing,
    closeForm,
    remove,
    dismissSuggestion,
  };
}

const billToastId = "bill-action";

/**
 * Shows a bill failure as a toast.
 * A 500 uses the sentence about the action, and in development the exception is the second line.
 * Validation text stays as the toast message.
 */
function reportBillFailure(error: unknown, action: string) {
  const text = describeApiError(error, action);
  showBillError(text.message, text.detail);
}

/**
 * Shows one bill error toast.
 * A later bill action replaces this toast.
 */
function showBillError(message: string, detail?: string) {
  toast.error(message, {
    id: billToastId,
    ...(detail ? { description: detail } : {}),
  });
}

/**
 * Orders bills by name, then by the next due date.
 * The page uses this after a save so a new bill does not stay at the bottom.
 */
function sortObligations(obligations: ObligationDto[]) {
  return [...obligations].sort(
    (left, right) =>
      left.name.localeCompare(right.name) ||
      left.nextDueDate.localeCompare(right.nextDueDate),
  );
}
