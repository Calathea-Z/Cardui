"use client";

import { useMemo, useState } from "react";
import { useConfirm } from "@/components/ui/confirm-dialog";
import {
  createCategory,
  deleteCategory,
  updateCategory,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { CategoryDto, SubGroupDto } from "@/lib/api/types";
import { emptyCategoryForm, type CategoryFormState } from "@/lib/categoryForm";
import { sortCategoriesByName } from "./categorySort";

/**
 * Holds the category list and the create or edit form.
 * The list stays sorted by name.
 */
export function useCategoriesManager(
  initialCategories: CategoryDto[],
  initialSubGroups: SubGroupDto[],
) {
  const [categories, setCategories] = useState(initialCategories);
  const [subGroups] = useState(initialSubGroups);
  const [form, setForm] = useState<CategoryFormState>(emptyCategoryForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const confirm = useConfirm();

  const sortedCategories = useMemo(
    () => sortCategoriesByName(categories),
    [categories],
  );

  const editingCategory = editingId
    ? (categories.find((category) => category.id === editingId) ?? null)
    : null;

  /**
   * Creates a category or saves the one being edited.
   * Name and sub-group are required, and a successful save clears the form.
   */
  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!form.subGroupId || !form.name.trim()) {
      setError("Name and sub-group are required.");
      return;
    }

    setError(null);
    setIsSaving(true);

    try {
      if (editingId) {
        const updatedCategory = await updateCategory(editingId, {
          name: form.name,
          color: form.color || null,
          icon: form.icon || null,
          subGroupId: form.subGroupId,
        });

        setCategories((current) =>
          current.map((category) =>
            category.id === updatedCategory.id ? updatedCategory : category,
          ),
        );
      } else {
        const createdCategory = await createCategory({
          name: form.name,
          color: form.color || null,
          icon: form.icon || null,
          subGroupId: form.subGroupId,
        });

        setCategories((current) => [...current, createdCategory]);
      }

      setForm(emptyCategoryForm);
      setEditingId(null);
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Fills the form from a category so the user can edit it.
   * A missing color uses #22c55e and a missing icon is blank.
   */
  function startEditing(category: CategoryDto) {
    setEditingId(category.id);
    setForm({
      name: category.name,
      color: category.color ?? "#22c55e",
      icon: category.icon ?? "",
      subGroupId: category.subGroupId,
    });
    setError(null);
  }

  /**
   * Leaves edit mode and clears the form.
   * Also clears any save error.
   */
  function cancelEditing() {
    setEditingId(null);
    setForm(emptyCategoryForm);
    setError(null);
  }

  /**
   * Deletes a custom category after the user confirms that its transactions become uncategorized.
   * A system category returns before that prompt, and deleting the category open in the form clears the form.
   */
  async function handleDelete(category: CategoryDto) {
    if (category.isSystem) {
      return;
    }

    const confirmed = await confirm({
      title: `Delete ${category.name}?`,
      description:
        "Transactions using this category will become uncategorized.",
      confirmLabel: "Delete",
    });

    if (!confirmed) {
      return;
    }

    setError(null);

    try {
      await deleteCategory(category.id);

      setCategories((current) =>
        current.filter((currentCategory) => currentCategory.id !== category.id),
      );

      if (editingId === category.id) {
        cancelEditing();
      }
    } catch (err) {
      setError(getApiErrorMessage(err));
    }
  }

  return {
    form,
    setForm,
    sortedCategories,
    subGroups,
    editingCategory,
    error,
    isSaving,
    handleSubmit,
    startEditing,
    cancelEditing,
    handleDelete,
  };
}

export type CategoriesManager = ReturnType<typeof useCategoriesManager>;
