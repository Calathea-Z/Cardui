"use client";

import { useState } from "react";
import { saveAllocationPreference } from "@/lib/api/browser/allocation";
import { describeApiError } from "@/lib/api/errors";
import type { AllocationPageDto } from "@/lib/api/types";
import {
  allocationFormFromPage,
  readAllocationForm,
  type AllocationFormState,
} from "./allocationRules";

/**
 * Holds the allocation page and its percent fields.
 * Saving stores the preference. It does not move money or change Plan's temporary preview.
 */
export function useAllocationPage(initial: AllocationPageDto) {
  const [page, setPage] = useState(initial);
  const [form, setForm] = useState<AllocationFormState>(() =>
    allocationFormFromPage(initial),
  );
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  /**
   * Replaces one percent field.
   * The stored page stays until save.
   */
  function setField(field: keyof AllocationFormState, value: string) {
    setSaveError(null);
    setForm((current) => ({ ...current, [field]: value }));
  }

  /**
   * Stores both rules and replaces the page with the saved preference.
   * An invalid field is not sent.
   */
  async function save() {
    const parsed = readAllocationForm(form);
    if (!parsed.ok) {
      setSaveError(parsed.error);
      return;
    }

    setIsSaving(true);
    setSaveError(null);
    try {
      const next = await saveAllocationPreference(parsed.dto);
      setPage(next);
      setForm(allocationFormFromPage(next));
    } catch (error) {
      const text = describeApiError(
        error,
        "The allocation could not be saved.",
      );
      setSaveError(
        text.detail ? `${text.message} ${text.detail}` : text.message,
      );
    } finally {
      setIsSaving(false);
    }
  }

  return { page, form, isSaving, saveError, setField, save };
}
