"use client";

import { useState } from "react";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import {
  createIncomeSource,
  deleteIncomeSource,
  updateIncomeSource,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
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
 * Amounts stay as one payment. A raise does not replace the current typical amount until the user confirms it.
 * The page does not compute a monthly equivalent.
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
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  const sortedSources = sortSources(sources);
  const today = calendarDateInTimeZone(timeZoneId, new Date());

  /**
   * Creates a source or saves the one being edited.
   * Typical pay, cadence, date, and reliability are required. Low, strong, and raises are checked before the request.
   */
  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const payload = toIncomeSourceUpsert(form);
    if (!payload.ok) {
      setError(payload.error);
      return;
    }

    const dto = payload.dto;

    setError(null);
    setNotice(null);
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

      setForm(emptyIncomeSourceForm());
      setEditingId(null);
    } catch (err) {
      setError(
        getApiErrorMessage(err, "That income source could not be saved."),
      );
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Fills the form from one active source.
   */
  function startEditing(source: IncomeSourceDto) {
    setError(null);
    setEditingId(source.id);
    setForm(incomeSourceToForm(source));
  }

  /**
   * Clears the form and leaves edit mode.
   */
  function cancelEditing() {
    setError(null);
    setEditingId(null);
    setForm(emptyIncomeSourceForm());
  }

  /**
   * Deletes a source after the user confirms.
   * The row and its expected raises are removed. Balances stay unchanged.
   */
  async function remove(source: IncomeSourceDto) {
    if (
      !window.confirm(`Remove ${source.name}? This deletes the income source.`)
    ) {
      return;
    }

    setError(null);
    setNotice(null);
    setBusyId(source.id);
    try {
      await deleteIncomeSource(source.id);
      setSources((current) => current.filter((item) => item.id !== source.id));
      if (editingId === source.id) {
        cancelEditing();
      }
    } catch (err) {
      setError(
        getApiErrorMessage(err, "That income source could not be removed."),
      );
    } finally {
      setBusyId(null);
    }
  }

  /**
   * Stores the raise amount as typical pay and removes that raise.
   * Other raises stay. Low or strong pay is cleared only when it no longer fits.
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
    },
    successNotice: string,
  ) {
    setError(null);
    setNotice(null);
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
      setNotice(successNotice);
    } catch (err) {
      setError(
        getApiErrorMessage(err, "That expected raise could not be updated."),
      );
    } finally {
      setBusyId(null);
    }
  }

  return {
    contributors,
    form,
    setForm,
    editingId,
    sources: sortedSources,
    today,
    error,
    notice,
    isSaving,
    busyId,
    handleSubmit,
    startEditing,
    cancelEditing,
    remove,
    confirmRaise,
    removeRaise,
  };
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
  },
): UpsertIncomeSourceDto {
  return {
    name: source.name,
    takeHomeAmount: amounts.takeHomeAmount,
    lowTakeHomeAmount: amounts.lowTakeHomeAmount,
    strongTakeHomeAmount: amounts.strongTakeHomeAmount,
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
