"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Alert } from "@/components/ui/alert";
import type { CategoryDto } from "@/lib/api/types";
import type { CategoryFormState } from "@/lib/categoryForm";

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
          <Input
            value={form.name}
            onChange={(event) =>
              onFormChange({ ...form, name: event.target.value })
            }
            className="mt-2 h-10"
            placeholder="Pets"
          />
        </label>

        <label className="block">
          <span className="text-sm text-muted-foreground">Color</span>
          <Input
            value={form.color}
            onChange={(event) =>
              onFormChange({ ...form, color: event.target.value })
            }
            className="mt-2 h-10"
            placeholder="#22c55e"
          />
        </label>

        <label className="block">
          <span className="text-sm text-muted-foreground">Icon</span>
          <Input
            value={form.icon}
            onChange={(event) =>
              onFormChange({ ...form, icon: event.target.value })
            }
            className="mt-2 h-10"
            placeholder="tag"
          />
        </label>

        {error ? <Alert variant="destructive">{error}</Alert> : null}

        <div className="flex gap-2">
          <Button type="submit" disabled={isSaving} size="lg">
            {isSaving
              ? "Saving"
              : editingCategory
                ? "Save changes"
                : "Create category"}
          </Button>

          {editingCategory ? (
            <Button type="button" variant="outline" size="lg" onClick={onCancel}>
              Cancel
            </Button>
          ) : null}
        </div>
      </div>
    </form>
  );
}
