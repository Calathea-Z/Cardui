"use client";

import { useState } from "react";
import { toast } from "sonner";
import { useConfirm } from "@/components/ui/confirm-dialog";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import {
  createIncomeSource,
  deleteIncomeSource,
  updateIncomeSource,
} from "@/lib/api/browser";
import { describeApiError } from "@/lib/api/errors";
import type {
  HouseholdContributorDto,
  IncomeRaiseDto,
  IncomeSourceDto,
  UpsertIncomeSourceDto,
} from "@/lib/api/types";
import {
  emptyIncomeSourceForm,
  incomeSourceToForm,
  toIncomeSourceUpsert,
  type IncomeSourceFormState,
} from "./incomeFormState";
import {
  amountsAfterConfirmingRaise,
  calendarDateInTimeZone,
  raiseRemovedMessage,
  typicalPayUpdatedMessage,
} from "./incomeRaiseReview";

/**
 * Holds the income list and the create or edit form.
 * Amounts stay as one payment. The form opens over the list.
 * A raise does not replace the current typical amount until the user confirms it.
 */
export function useIncomeSources(
  initialSources: IncomeSourceDto[],
  contributors: HouseholdContributorDto[],
  timeZoneId: string,
) {
  const [sources, setSources] = useState(initialSources);
  const [form, setForm] = useState<IncomeSourceFormState>(
    emptyIncomeSourceForm(),
  );
  const [editingId, setEditingId] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const confirm = useConfirm();

  const sortedSources = sortSources(sources);
  const today = calendarDateInTimeZone(timeZoneId, new Date());

  /**
   * Creates a source or saves the one being edited.
   * Returns true when the source was saved. Typical pay, cadence, date, and reliability are required.
   */
  async function handleSubmit(
    event: React.FormEvent<HTMLFormElement>,
  ): Promise<boolean> {
    event.preventDefault();
    const payload = toIncomeSourceUpsert(form);
    if (!payload.ok) {
      showIncomeError(payload.error);
      return false;
    }

    const dto = payload.dto;

    toast.dismiss(incomeToastId);
    setIsSaving(true);

    try {
      if (editingId) {
        const updated = await updateIncomeSource(editingId, dto);
        setSources((current) =>
          current.map((source) =>
            source.id === updated.id ? updated : source,
          ),
        );
      } else {
        const created = await createIncomeSource(dto);
        setSources((current) => [...current, created]);
      }

      return true;
    } catch (err) {
      reportIncomeFailure(err, "That income source could not be saved.");
      return false;
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Opens a blank form for a new source.
   */
  function startAdding() {
    toast.dismiss(incomeToastId);
    setEditingId(null);
    setForm(emptyIncomeSourceForm());
    setIsFormOpen(true);
  }

  /**
   * Opens the form with one saved source.
   */
  function startEditing(source: IncomeSourceDto) {
    toast.dismiss(incomeToastId);
    setEditingId(source.id);
    setForm(incomeSourceToForm(source));
    setIsFormOpen(true);
  }

  /**
   * Closes the form.
   * The next open replaces whatever was left in the fields.
   */
  function closeForm() {
    toast.dismiss(incomeToastId);
    setIsFormOpen(false);
  }

  /**
   * Deletes a source after the user confirms.
   * The row and its expected raises are removed. Balances stay unchanged.
   */
  async function remove(source: IncomeSourceDto) {
    const confirmed = await confirm({
      title: `Remove ${source.name}?`,
      description: "This deletes the income source.",
      confirmLabel: "Remove",
    });
    if (!confirmed) {
      return;
    }

    setBusyId(source.id);
    try {
      await deleteIncomeSource(source.id);
      setSources((current) => current.filter((item) => item.id !== source.id));
      if (editingId === source.id) {
        closeForm();
      }
    } catch (err) {
      reportIncomeFailure(err, "That income source could not be removed.");
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Stores the raise amount as typical pay and removes that raise.
   * Other raises stay. Low, strong, or gross pay is cleared only when it no longer fits.
   */
  async function confirmRaise(source: IncomeSourceDto, raise: IncomeRaiseDto) {
    const amounts = amountsAfterConfirmingRaise(source, raise.takeHomeAmount);
    await saveRaiseDecision(
      source,
      raise,
      amounts,
      typicalPayUpdatedMessage(
        source.name,
        formatCurrency(amounts.takeHomeAmount, source.currency),
        amounts.clearedLow,
        amounts.clearedStrong,
        amounts.clearedGross,
      ),
    );
  }

  /**
   * Removes one expected raise.
   * Typical pay stays the amount already stored.
   */
  async function removeRaise(source: IncomeSourceDto, raise: IncomeRaiseDto) {
    await saveRaiseDecision(
      source,
      raise,
      {
        takeHomeAmount: source.takeHomeAmount,
        lowTakeHomeAmount: source.lowTakeHomeAmount,
        strongTakeHomeAmount: source.strongTakeHomeAmount,
        grossPayAmount: source.grossPayAmount,
      },
      raiseRemovedMessage(
        source.name,
        formatCurrency(source.takeHomeAmount, source.currency),
      ),
    );
  }

  /**
   * Saves a source after one raise is resolved.
   * The open form for that source is replaced with the saved row.
   */
  async function saveRaiseDecision(
    source: IncomeSourceDto,
    raise: IncomeRaiseDto,
    amounts: {
      takeHomeAmount: number;
      lowTakeHomeAmount: number | null;
      strongTakeHomeAmount: number | null;
      grossPayAmount: number | null;
    },
    successNotice: string,
  ) {
    setBusyId(source.id);
    try {
      const updated = await updateIncomeSource(
        source.id,
        sourceToUpsert(source, raise.id, amounts),
      );
      setSources((current) =>
        current.map((item) => (item.id === updated.id ? updated : item)),
      );
      if (editingId === source.id) {
        setForm(incomeSourceToForm(updated));
      }
      reportIncomeNotice(successNotice);
    } catch (err) {
      reportIncomeFailure(err, "That expected raise could not be updated.");
    } finally {
      setBusyId(null);
    }
  }

  return {
    contributors,
    form,
    setForm,
    editingId,
    isFormOpen,
    sources: sortedSources,
    today,
    isSaving,
    busyId,
    handleSubmit,
    startAdding,
    startEditing,
    closeForm,
    remove,
    confirmRaise,
    removeRaise,
  };
}

const incomeToastId = "income-action";

/**
 * Shows an income failure as a toast.
 * A 500 uses the sentence about the action, and in development the exception is the second line.
 * Validation text stays as the toast message.
 */
function reportIncomeFailure(error: unknown, action: string) {
  const text = describeApiError(error, action);
  showIncomeError(text.message, text.detail);
}

/**
 * Shows one income error toast.
 * A later income action replaces this toast.
 */
function showIncomeError(message: string, detail?: string) {
  toast.error(message, {
    id: incomeToastId,
    ...(detail ? { description: detail } : {}),
  });
}

/**
 * Shows the result of confirming or removing a raise.
 * A later income action replaces this toast.
 */
function reportIncomeNotice(message: string) {
  toast.success(message, { id: incomeToastId });
}

/**
 * Builds the save payload for a stored source with one raise left out.
 * The payment date keeps the calendar day.
 */
function sourceToUpsert(
  source: IncomeSourceDto,
  omitRaiseId: string,
  amounts: {
    takeHomeAmount: number;
    lowTakeHomeAmount: number | null;
    strongTakeHomeAmount: number | null;
    grossPayAmount: number | null;
  },
): UpsertIncomeSourceDto {
  return {
    name: source.name,
    takeHomeAmount: amounts.takeHomeAmount,
    lowTakeHomeAmount: amounts.lowTakeHomeAmount,
    strongTakeHomeAmount: amounts.strongTakeHomeAmount,
    grossPayAmount: amounts.grossPayAmount,
    cadence: source.cadence,
    nextPaymentDate: source.nextPaymentDate.slice(0, 10),
    contributorId: source.contributorId,
    reliability: source.reliability,
    raises: source.raises
      .filter((raise) => raise.id !== omitRaiseId)
      .map((raise) => ({
        effectiveDate: raise.effectiveDate.slice(0, 10),
        takeHomeAmount: raise.takeHomeAmount,
      })),
  };
}

/**
 * Orders sources by name, then by next payment date.
 * The page uses this after a save so a new source does not stay at the bottom.
 */
function sortSources(sources: IncomeSourceDto[]) {
  return [...sources].sort(
    (left, right) =>
      left.name.localeCompare(right.name) ||
      left.nextPaymentDate.localeCompare(right.nextPaymentDate),
  );
}
