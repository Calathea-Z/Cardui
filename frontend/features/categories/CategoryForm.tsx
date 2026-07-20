"use client";

import type { CategoryDto } from "@/lib/api";
import type { CategoryFormState } from "@/lib/categoryForm";
import { CategoryErrorBanner } from "./CategoryErrorBanner";

type CategoryFormProps = {
  form: CategoryFormState;
  onFormChange: (form: CategoryFormState) => void;
  editingCategory: CategoryDto | null;
  error: string | null;
  isSaving: boolean;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
  onCancel: () => void;
};

export function CategoryForm({
  form,
  onFormChange,
  editingCategory,
  error,
  isSaving,
  onSubmit,
  onCancel,
}: CategoryFormProps) {
  return (
    <form onSubmit={onSubmit} className="app-panel p-5">
      <h2 className="font-semibold">
        {editingCategory
          ? editingCategory.isSystem
            ? "Edit system category"
            : "Edit category"
          : "New category"}
      </h2>

      {editingCategory?.isSystem ? (
        <p className="mt-2 text-sm text-muted-foreground">
          You can customize the display name, color, and icon. System categories
          cannot be deleted.
        </p>
      ) : null}

      <div className="mt-5 space-y-4">
        <label className="block">
          <span className="text-sm text-muted-foreground">Name</span>
          <input
            value={form.name}
            onChange={(event) =>
              onFormChange({ ...form, name: event.target.value })
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
              onFormChange({ ...form, color: event.target.value })
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
              onFormChange({ ...form, icon: event.target.value })
            }
            className="app-input mt-2 h-10 w-full"
            placeholder="tag"
          />
        </label>

        {error ? <CategoryErrorBanner message={error} /> : null}

        <div className="flex gap-2">
          <button type="submit" disabled={isSaving} className="app-cta-button">
            {isSaving
              ? "Saving"
              : editingCategory
                ? "Save changes"
                : "Create category"}
          </button>

          {editingCategory ? (
            <button
              type="button"
              onClick={onCancel}
              className="cursor-pointer rounded-md border border-border px-4 py-2 text-sm text-muted-foreground transition hover:bg-accent"
            >
              Cancel
            </button>
          ) : null}
        </div>
      </div>
    </form>
  );
}
