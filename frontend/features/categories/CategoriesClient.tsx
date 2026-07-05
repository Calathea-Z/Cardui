"use client";

import { useMemo, useState } from "react";
import {
  createCategory,
  deleteCategory,
  getApiErrorMessage,
  updateCategory,
  type CategoryDto,
} from "@/lib/api";

type CategoriesClientProps = {
  initialCategories: CategoryDto[];
};

type CategoryFormState = {
  name: string;
  color: string;
  icon: string;
};

const emptyForm: CategoryFormState = {
  name: "",
  color: "#22c55e",
  icon: "",
};

export function CategoriesClient({ initialCategories }: CategoriesClientProps) {
  const [categories, setCategories] = useState(initialCategories);
  const [form, setForm] = useState<CategoryFormState>(emptyForm);
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

      setForm(emptyForm);
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
    setForm(emptyForm);
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
        <p className="text-sm text-slate-400">Organize spending</p>
        <h1 className="text-3xl font-semibold">Categories</h1>
      </div>

      <div className="grid gap-6 lg:grid-cols-[360px_1fr]">
        <form
          onSubmit={handleSubmit}
          className="rounded-lg border border-slate-800 bg-slate-900 p-5"
        >
          <h2 className="font-semibold">
            {editingCategory
              ? editingCategory.isSystem
                ? "Edit system category"
                : "Edit category"
              : "New category"}
          </h2>

          {editingCategory?.isSystem ? (
            <p className="mt-2 text-sm text-slate-400">
              You can customize the display name, color, and icon. System
              categories cannot be deleted.
            </p>
          ) : null}

          <div className="mt-5 space-y-4">
            <label className="block">
              <span className="text-sm text-slate-400">Name</span>
              <input
                value={form.name}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    name: event.target.value,
                  }))
                }
                className="mt-2 h-10 w-full rounded-md border border-slate-700 bg-slate-950 px-3 text-sm outline-none transition focus:border-emerald-400"
                placeholder="Pets"
              />
            </label>

            <label className="block">
              <span className="text-sm text-slate-400">Color</span>
              <input
                value={form.color}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    color: event.target.value,
                  }))
                }
                className="mt-2 h-10 w-full rounded-md border border-slate-700 bg-slate-950 px-3 text-sm outline-none transition focus:border-emerald-400"
                placeholder="#22c55e"
              />
            </label>

            <label className="block">
              <span className="text-sm text-slate-400">Icon</span>
              <input
                value={form.icon}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    icon: event.target.value,
                  }))
                }
                className="mt-2 h-10 w-full rounded-md border border-slate-700 bg-slate-950 px-3 text-sm outline-none transition focus:border-emerald-400"
                placeholder="tag"
              />
            </label>

            {error ? (
              <p className="rounded-md border border-rose-500/40 bg-rose-500/10 px-3 py-2 text-sm text-rose-200">
                {error}
              </p>
            ) : null}

            <div className="flex gap-2">
              <button
                type="submit"
                disabled={isSaving}
                className="cursor-pointer rounded-md bg-emerald-500 px-4 py-2 text-sm font-medium text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-60"
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
                  className="cursor-pointer rounded-md border border-slate-700 px-4 py-2 text-sm text-slate-300 transition hover:bg-slate-800"
                >
                  Cancel
                </button>
              ) : null}
            </div>
          </div>
        </form>

        <div className="overflow-hidden rounded-lg border border-slate-800 bg-slate-900">
          <div className="grid grid-cols-[1fr_120px_160px] gap-4 border-b border-slate-800 px-4 py-3 text-sm font-medium text-slate-400">
            <span>Name</span>
            <span>Type</span>
            <span className="text-right">Actions</span>
          </div>

          <div className="divide-y divide-slate-800">
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
                      <p className="text-xs text-slate-500">
                        Icon: {category.icon}
                      </p>
                    ) : null}
                  </div>
                </div>

                <span className="text-slate-400">
                  {category.isSystem ? "System" : "Custom"}
                </span>

                <div className="flex justify-end gap-2">
                  <button
                    type="button"
                    onClick={() => startEditing(category)}
                    className="cursor-pointer rounded-md border border-slate-700 px-3 py-1.5 text-xs text-slate-300 transition hover:bg-slate-800"
                  >
                    Edit
                  </button>

                  <button
                    type="button"
                    disabled={category.isSystem}
                    onClick={() => handleDelete(category)}
                    className="cursor-pointer rounded-md border border-rose-500/40 px-3 py-1.5 text-xs text-rose-200 transition hover:bg-rose-500/10 disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}

            {sortedCategories.length === 0 ? (
              <div className="px-4 py-12 text-center text-sm text-slate-400">
                No categories yet.
              </div>
            ) : null}
          </div>
        </div>
      </div>
    </section>
  );
}
