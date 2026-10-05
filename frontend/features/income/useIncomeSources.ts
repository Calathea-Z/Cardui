"use client";

import { useState } from "react";
import { parseMoney } from "@/features/accounts/manualAccount";
import {
  createIncomeSource,
  deleteIncomeSource,
  updateIncomeSource,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type {
  HouseholdContributorDto,
  IncomeSourceDto,
  UpsertIncomeSourceDto,
} from "@/lib/api/types";
import {
  emptyIncomeSourceForm,
  incomeSourceToForm,
  type IncomeSourceFormState,
} from "./incomeFormState";

/**
 * Holds the income list and the create or edit form.
 * Amounts stay as one payment. The page does not compute a monthly equivalent.
 */
export function useIncomeSources(
  initialSources: IncomeSourceDto[],
  contributors: HouseholdContributorDto[],
) {
  const [sources, setSources] = useState(initialSources);
  const [form, setForm] = useState<IncomeSourceFormState>(
    emptyIncomeSourceForm(),
  );
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  const sortedSources = sortSources(sources);

  /**
   * Creates a source or saves the one being edited.
   * Name, a positive take-home amount, cadence, date, and reliability are required.
   */
  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const dto = toUpsert(form);
    if (!dto) {
      setError(
        "Enter a name, the net pay for one payment, how often it is paid, the next date, and how reliable it is.",
      );
      return;
    }

    setError(null);
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
   * The row is removed. Balances stay unchanged.
   */
  async function remove(source: IncomeSourceDto) {
    if (
      !window.confirm(`Remove ${source.name}? This deletes the income source.`)
    ) {
      return;
    }

    setError(null);
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

  return {
    contributors,
    form,
    setForm,
    editingId,
    sources: sortedSources,
    error,
    isSaving,
    busyId,
    handleSubmit,
    startEditing,
    cancelEditing,
    remove,
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

/**
 * Builds the save payload from the form.
 * A blank, zero, or incomplete form returns null so the caller can ask for the missing facts.
 */
function toUpsert(form: IncomeSourceFormState): UpsertIncomeSourceDto | null {
  const takeHomeAmount = parseMoney(form.takeHomeAmount);
  if (
    !form.name.trim() ||
    takeHomeAmount === null ||
    takeHomeAmount <= 0 ||
    !form.cadence ||
    !form.nextPaymentDate ||
    !form.reliability
  ) {
    return null;
  }

  return {
    name: form.name.trim(),
    takeHomeAmount,
    cadence: form.cadence,
    nextPaymentDate: form.nextPaymentDate,
    contributorId: form.contributorId || null,
    reliability: form.reliability,
  };
}
