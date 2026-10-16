"use client";

import { useState } from "react";
import { compareRestructuring } from "@/lib/api/browser/restructuring";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { RestructuringComparisonDto } from "@/lib/api/types";
import {
  initialRestructuringForm,
  readRestructuringForm,
  sameRestructuringForm,
  type RestructuringFieldErrors,
  type RestructuringFormDebt,
  type RestructuringFormState,
} from "./restructuringRules";

/**
 * Holds one offer comparison on Plan.
 * The form and the last result stay while other Plan views are open. Nothing is saved.
 */
export function useRestructuringComparison() {
  const [form, setForm] = useState<RestructuringFormState>(
    initialRestructuringForm,
  );
  const [comparison, setComparison] =
    useState<RestructuringComparisonDto | null>(null);
  const [comparedForm, setComparedForm] =
    useState<RestructuringFormState | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<RestructuringFieldErrors>({});
  const [isComparing, setIsComparing] = useState(false);

  /**
   * Replaces one text or choice field.
   * The last comparison stays until Compare offer runs again.
   */
  function setField<Key extends keyof RestructuringFormState>(
    field: Key,
    value: RestructuringFormState[Key],
  ) {
    setForm((current) => ({ ...current, [field]: value }));
    setFieldErrors((current) => clearOfferField(current, field));
  }

  /**
   * Adds or removes one debt from the offer.
   */
  function toggleDebt(debtId: string) {
    setForm((current) => ({
      ...current,
      debtIds: current.debtIds.includes(debtId)
        ? current.debtIds.filter((id) => id !== debtId)
        : [...current.debtIds, debtId],
    }));
    setFieldErrors((current) => {
      const next = { ...current };
      delete next.debts;
      return next;
    });
  }

  /**
   * Compares the form with the household's current debts.
   * An invalid field is not sent. A failed request keeps the previous comparison.
   */
  async function compare(
    debts: RestructuringFormDebt[],
    planningCurrency: string,
  ) {
    const parsed = readRestructuringForm(form, debts, planningCurrency);
    if (!parsed.ok) {
      setError(parsed.error);
      setFieldErrors(parsed.fields);
      return;
    }

    setIsComparing(true);
    setError(null);
    setFieldErrors({});
    try {
      const next = await compareRestructuring(parsed.dto);
      setComparison(next);
      setComparedForm(form);
    } catch (caught) {
      setError(getApiErrorMessage(caught, "The comparison could not be made."));
    } finally {
      setIsComparing(false);
    }
  }

  const stale =
    comparison !== null &&
    comparedForm !== null &&
    !sameRestructuringForm(form, comparedForm);

  return {
    form,
    comparison,
    error,
    fieldErrors,
    isComparing,
    stale,
    setField,
    toggleDebt,
    compare,
  };
}

/**
 * Drops the message for the field that just changed.
 * Other field messages stay until the next comparison.
 */
function clearOfferField(
  errors: RestructuringFieldErrors,
  field: keyof RestructuringFormState,
): RestructuringFieldErrors {
  if (
    field !== "apr" &&
    field !== "termMonths" &&
    field !== "fee" &&
    field !== "upfrontCash"
  ) {
    return errors;
  }

  const next = { ...errors };
  delete next[field];
  return next;
}
