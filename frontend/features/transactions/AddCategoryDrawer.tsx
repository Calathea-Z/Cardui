"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { createCategory } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import { emptyCategoryForm, type CategoryFormState } from "@/lib/categoryForm";
import type { CategoryDto } from "@/lib/api/types";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";

type AddCategoryDrawerProps = {
  open: boolean;
  onClose: () => void;
  onCreated: (category: CategoryDto) => void;
};

export function AddCategoryDrawer({
  open,
  onClose,
  onCreated,
}: AddCategoryDrawerProps) {
  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title="New Category"
      headerAction="close"
      overlayClassName="z-80"
      className={FULL_SCREEN_SHEET_CLASSNAME}
    >
      {open ? (
        <AddCategoryDrawerContent onClose={onClose} onCreated={onCreated} />
      ) : null}
    </BottomSheet>
  );
}

function AddCategoryDrawerContent({
  onClose,
  onCreated,
}: {
  onClose: () => void;
  onCreated: (category: CategoryDto) => void;
}) {
  const [form, setForm] = useState<CategoryFormState>(emptyCategoryForm);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const name = form.name.trim();
    if (!name || isSaving) {
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const category = await createCategory({
        name,
        color: form.color || null,
        icon: form.icon || null,
      });
      onCreated(category);
    } catch (err) {
      setError(getApiErrorMessage(err, "Could not create category."));
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <label className="block">
        <span className="text-sm text-muted-foreground">Name</span>
        <Input
          value={form.name}
          onChange={(event) =>
            setForm((current) => ({ ...current, name: event.target.value }))
          }
          className="mt-2 h-10"
          placeholder="Pets"
          autoFocus
          disabled={isSaving}
        />
      </label>

      <label className="block">
        <span className="text-sm text-muted-foreground">Color</span>
        <Input
          value={form.color}
          onChange={(event) =>
            setForm((current) => ({ ...current, color: event.target.value }))
          }
          className="mt-2 h-10"
          placeholder="#22c55e"
          disabled={isSaving}
        />
      </label>

      <label className="block">
        <span className="text-sm text-muted-foreground">Icon</span>
        <Input
          value={form.icon}
          onChange={(event) =>
            setForm((current) => ({ ...current, icon: event.target.value }))
          }
          className="mt-2 h-10"
          placeholder="tag"
          disabled={isSaving}
        />
      </label>

      {error ? <Alert variant="destructive">{error}</Alert> : null}

      <Button type="submit" disabled={isSaving || !form.name.trim()} size="lg">
        {isSaving ? "Creating" : "Create category"}
      </Button>
    </form>
  );
}
