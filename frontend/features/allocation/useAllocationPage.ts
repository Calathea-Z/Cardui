"use client";

import { useState } from "react";
import { toast } from "sonner";
import { saveAllocationPreference } from "@/lib/api/browser/allocation";
import { describeApiError } from "@/lib/api/errors";
import type { AllocationPageDto } from "@/lib/api/types";
import {
  allocationFormFromPage,
  readAllocationForm,
  type AllocationFormState,
} from "./allocationRules";

const allocationToastId = "allocation-action";

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

  /**
   * Replaces one percent field.
   * The stored page stays until save.
   */
  function setField(field: keyof AllocationFormState, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  /**
   * Stores both rules and replaces the page with the saved preference.
   * An invalid field is not sent.
   */
  async function save() {
    const parsed = readAllocationForm(form);
    if (!parsed.ok) {
      toast.error(parsed.error, { id: allocationToastId });
      return;
    }

    setIsSaving(true);
    try {
      const next = await saveAllocationPreference(parsed.dto);
      setPage(next);
      setForm(allocationFormFromPage(next));
      toast.success("Allocation saved.", { id: allocationToastId });
    } catch (error) {
      const text = describeApiError(
        error,
        "The allocation could not be saved.",
      );
      toast.error(text.message, {
        id: allocationToastId,
        ...(text.detail ? { description: text.detail } : {}),
      });
    } finally {
      setIsSaving(false);
    }
  }

  return { page, form, isSaving, setField, save };
}
