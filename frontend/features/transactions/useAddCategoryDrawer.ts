"use client";

import { useState } from "react";
import { sortByOrderThenName } from "@/features/categories/categorySort";
import { createCategory } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { CategoryDto, GroupDto, SubGroupDto } from "@/lib/api/types";

type AddCategoryFormState = {
  name: string;
  emoji: string;
  color: string;
  groupId: string;
  subGroupId: string;
};

type UseAddCategoryDrawerOptions = {
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  initialSubGroupId: string;
  onCreated: (category: CategoryDto) => void;
};

/**
 * Builds a blank category form.
 * A known starting subgroup also selects its group.
 */
function createInitialForm(
  initialSubGroupId: string,
  subGroups: SubGroupDto[],
): AddCategoryFormState {
  const initialSubGroup = subGroups.find(
    (subGroup) => subGroup.id === initialSubGroupId,
  );

  return {
    name: "",
    emoji: "",
    color: "",
    groupId: initialSubGroup?.groupId ?? "",
    subGroupId: initialSubGroupId,
  };
}

/**
 * Holds the add-category draft and creates the category.
 * Save stays off until every field is filled and the form differs from its start.
 */
export function useAddCategoryDrawer({
  groups,
  subGroups,
  initialSubGroupId,
  onCreated,
}: UseAddCategoryDrawerOptions) {
  const [initialForm] = useState(() =>
    createInitialForm(initialSubGroupId, subGroups),
  );
  const [form, setForm] = useState<AddCategoryFormState>(initialForm);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const sortedGroups = sortByOrderThenName(groups);
  const visibleSubGroups = sortByOrderThenName(
    subGroups.filter((subGroup) => subGroup.groupId === form.groupId),
  );

  const effectiveSubGroupId =
    form.subGroupId &&
    visibleSubGroups.some((subGroup) => subGroup.id === form.subGroupId)
      ? form.subGroupId
      : "";

  const isDirty =
    form.name !== initialForm.name ||
    form.emoji !== initialForm.emoji ||
    form.color !== initialForm.color ||
    form.groupId !== initialForm.groupId ||
    form.subGroupId !== initialForm.subGroupId;

  const isComplete =
    form.name.trim().length > 0 &&
    form.emoji.length > 0 &&
    form.color.length > 0 &&
    form.groupId.length > 0 &&
    effectiveSubGroupId.length > 0;

  const canSave = isDirty && isComplete && !isSaving;

  /**
   * Creates the category when the form is complete and has been changed.
   * An incomplete form shows the missing facts and skips the request. The name is trimmed, and the subgroup must still belong to the chosen group.
   */
  async function submit() {
    if (!isComplete) {
      setError("Enter a name, emoji, color, group, and sub-group.");
      return;
    }

    if (!canSave) {
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const category = await createCategory({
        name: form.name.trim(),
        subGroupId: effectiveSubGroupId,
        color: form.color,
        icon: form.emoji,
      });
      onCreated(category);
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not create category."));
    } finally {
      setIsSaving(false);
    }
  }

  return {
    form,
    setForm,
    error,
    isSaving,
    canSave,
    sortedGroups,
    visibleSubGroups,
    effectiveSubGroupId,
    submit,
  };
}
