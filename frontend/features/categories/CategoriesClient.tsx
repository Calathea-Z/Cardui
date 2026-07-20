"use client";

import { useMemo, useState } from "react";
import {
  createCategory,
  deleteCategory,
  getApiErrorMessage,
  updateCategory,
  type CategoryDto,
} from "@/lib/api";
import { emptyCategoryForm, type CategoryFormState } from "@/lib/categoryForm";

type CategoriesClientProps = {
  initialCategories: CategoryDto[];
};

export function CategoriesClient({ initialCategories }: CategoriesClientProps) {
  const [categories, setCategories] = useState(initialCategories);
  const [form, setForm] = useState<CategoryFormState>(emptyCategoryForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const sortedCategories = useMemo(
    () => categories.slice().sort((a, b) => a.name.localeCompare(b.name)),
    [categories],
  );

  const editingCategory = editingId
    ? categories.find((category) => category.id === editingId)
    : null;

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setError(null);
    setIsSaving(true);

    try {
      if (editingId) {
        const updatedCategory = await updateCategory(editingId, {
          name: form.name,
          color: form.color || null,
          icon: form.icon || null,
          parentCategoryId: null,
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
          parentCategoryId: null,
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

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <div>
        <p className="text-sm text-muted-foreground">Organize spending</p>
        <h1 className="text-3xl font-semibold text-foreground">Categories</h1>
      </div>

      <div className="grid gap-6 lg:grid-cols-[360px_1fr]">
        <form
          onSubmit={handleSubmit}
          className="app-panel p-5"
        >
          <h2 className="font-semibold">
            {editingCategory
              ? editingCategory.isSystem
                ? "Edit system category"
                : "Edit category"
              : "New category"}
          </h2>

          {editingCategory?.isSystem ? (
            <p className="mt-2 text-sm text-muted-foreground">
              You can customize the display name, color, and icon. System
              categories cannot be deleted.
            </p>
          ) : null}

          <div className="mt-5 space-y-4">
            <label className="block">
              <span className="text-sm text-muted-foreground">Name</span>
              <input
                value={form.name}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    name: event.target.value,
                  }))
                }
                className="app-input mt-2 h-10 w-full"
                placeholder="Pets"
              />
            </label>

            <label className="block">
              <span className="text-sm text-muted-foreground">Color</span>
              <input
                value={form.color}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    color: event.target.value,
                  }))
                }
                className="app-input mt-2 h-10 w-full"
                placeholder="#22c55e"
              />
            </label>

            <label className="block">
              <span className="text-sm text-muted-foreground">Icon</span>
              <input
                value={form.icon}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    icon: event.target.value,
                  }))
                }
                className="app-input mt-2 h-10 w-full"
                placeholder="tag"
              />
            </label>

            {error ? (
              <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {error}
              </p>
            ) : null}

            <div className="flex gap-2">
              <button
                type="submit"
                disabled={isSaving}
                className="app-cta-button"
              >
                {isSaving
                  ? "Saving"
                  : editingCategory
                    ? "Save changes"
                    : "Create category"}
              </button>

              {editingCategory ? (
                <button
                  type="button"
                  onClick={cancelEditing}
                  className="cursor-pointer rounded-md border border-border px-4 py-2 text-sm text-muted-foreground transition hover:bg-accent"
                >
                  Cancel
                </button>
              ) : null}
            </div>
          </div>
        </form>

        <div className="app-panel overflow-hidden">
          <div className="grid grid-cols-[1fr_120px_160px] gap-4 border-b border-border px-4 py-3 text-sm font-medium text-muted-foreground">
            <span>Name</span>
            <span>Type</span>
            <span className="text-right">Actions</span>
          </div>

          <div className="divide-y divide-border/70">
            {sortedCategories.map((category) => (
              <div
                key={category.id}
                className="grid grid-cols-[1fr_120px_160px] items-center gap-4 px-4 py-4 text-sm"
              >
                <div className="flex min-w-0 items-center gap-3">
                  <span
                    className="h-3 w-3 rounded-full"
                    style={{ backgroundColor: category.color ?? "#64748b" }}
                  />
                  <div className="min-w-0">
                    <p className="truncate font-medium">{category.name}</p>
                    {category.icon ? (
                      <p className="text-xs text-muted-foreground">
                        Icon: {category.icon}
                      </p>
                    ) : null}
                  </div>
                </div>

                <span className="text-muted-foreground">
                  {category.isSystem ? "System" : "Custom"}
                </span>

                <div className="flex justify-end gap-2">
                  <button
                    type="button"
                    onClick={() => startEditing(category)}
                    className="cursor-pointer rounded-md border border-border px-3 py-1.5 text-xs text-muted-foreground transition hover:bg-accent"
                  >
                    Edit
                  </button>

                  <button
                    type="button"
                    disabled={category.isSystem}
                    onClick={() => handleDelete(category)}
                    className="cursor-pointer rounded-md border border-destructive/40 px-3 py-1.5 text-xs text-destructive transition hover:bg-destructive/10 disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}

            {sortedCategories.length === 0 ? (
              <div className="px-4 py-12 text-center text-sm text-muted-foreground">
                No categories yet.
              </div>
            ) : null}
          </div>
        </div>
      </div>
    </section>
  );
}
