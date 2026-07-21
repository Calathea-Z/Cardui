"use client";

import { useMemo, useState } from "react";
import {
  createCategory,
  deleteCategory,
  updateCategory,
} from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { CategoryDto, SubGroupDto } from "@/lib/api/types";
import { emptyCategoryForm, type CategoryFormState } from "@/lib/categoryForm";
import { sortCategoriesByName } from "./categorySort";

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

  const sortedCategories = useMemo(
    () => sortCategoriesByName(categories),
    [categories],
  );

  const editingCategory = editingId
    ? (categories.find((category) => category.id === editingId) ?? null)
    : null;

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

  function cancelEditing() {
    setEditingId(null);
    setForm(emptyCategoryForm);
    setError(null);
  }

  async function handleDelete(category: CategoryDto) {
    if (category.isSystem) {
      return;
    }

    const confirmed = window.confirm(
      `Delete ${category.name}? Transactions using this category will become uncategorized.`,
    );

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
