"use client";

import { ChevronDown } from "lucide-react";
import { useRef, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Form } from "@/components/ui/form";
import { Button } from "@/components/ui/button";
import { ChoiceSurface } from "@/components/ui/choice-surface";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import type { CategoryDto, GroupDto, SubGroupDto } from "@/lib/api/types";
import type { CategoryFormState } from "@/lib/categoryForm";
import { cn } from "@/lib/utils";
import { getCategoryEmoji } from "./categoryEmoji";
import { sortByOrderThenName } from "./categorySort";
import { ColorPicker, colorPickerPopoverSize } from "./ColorPicker";
import { EmojiPicker, emojiPickerPopoverSize } from "./EmojiPicker";

const fieldTriggerClassName = cn(
  "mt-2 flex h-10 w-full min-w-0 items-center justify-between gap-2 rounded-lg border border-border bg-card px-2.5 text-left text-sm transition-colors outline-none",
  "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
  "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
);

type CategoryFormProps = {
  form: CategoryFormState;
  onFormChange: (form: CategoryFormState) => void;
  editingCategory: CategoryDto | null;
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  error: string | null;
  isSaving: boolean;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
  onCancel: () => void;
};

/**
 * Form for creating or editing a category.
 * The group and sub-group lock while saving or when the category is a system category, and a save needs a name and sub-group. Color and icon open the same pickers as add category.
 */
export function CategoryForm({
  form,
  onFormChange,
  editingCategory,
  groups,
  subGroups,
  error,
  isSaving,
  onSubmit,
  onCancel,
}: CategoryFormProps) {
  const sortedGroups = sortByOrderThenName(groups);

  const selectedSubGroup = subGroups.find(
    (subGroup) => subGroup.id === form.subGroupId,
  );
  const selectedGroupId = selectedSubGroup?.groupId ?? "";

  const visibleSubGroups = sortByOrderThenName(
    subGroups.filter((subGroup) =>
      selectedGroupId ? subGroup.groupId === selectedGroupId : false,
    ),
  );

  const lockHierarchy = isSaving || Boolean(editingCategory?.isSystem);
  const [colorOpen, setColorOpen] = useState(false);
  const [iconOpen, setIconOpen] = useState(false);
  const colorTriggerRef = useRef<HTMLButtonElement>(null);
  const iconTriggerRef = useRef<HTMLButtonElement>(null);
  const iconGlyph = iconOnTheControl(form.icon);

  return (
    <Form onSubmit={onSubmit} className="app-panel p-5">
      <h2 className="font-semibold">
        {editingCategory
          ? editingCategory.isSystem
            ? "Edit system category"
            : "Edit category"
          : "New category"}
      </h2>

      {editingCategory?.isSystem ? (
        <p className="mt-2 text-sm text-muted-foreground">
          System categories are shared and cannot be changed.
        </p>
      ) : null}

      <div className="mt-5 space-y-4">
        <div className="block">
          <span className="text-sm text-muted-foreground">Group</span>
          <Select
            title="Group"
            value={selectedGroupId}
            disabled={lockHierarchy}
            placeholder="Select a group"
            className="mt-2"
            onChange={(groupId) => {
              const firstSubGroup = subGroups
                .filter((subGroup) => subGroup.groupId === groupId)
                .sort(
                  (left, right) =>
                    left.sortOrder - right.sortOrder ||
                    left.name.localeCompare(right.name),
                )[0];

              onFormChange({
                ...form,
                subGroupId: firstSubGroup?.id ?? "",
              });
            }}
            options={sortedGroups.map((group) => ({
              value: group.id,
              label: group.name,
            }))}
          />
        </div>

        <div className="block">
          <span className="text-sm text-muted-foreground">Sub-group</span>
          <Select
            title="Sub-group"
            value={form.subGroupId}
            disabled={lockHierarchy || !selectedGroupId}
            placeholder="Select a sub-group"
            className="mt-2"
            onChange={(subGroupId) => onFormChange({ ...form, subGroupId })}
            options={visibleSubGroups.map((subGroup) => ({
              value: subGroup.id,
              label: subGroup.name,
            }))}
          />
        </div>

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

        <div className="block">
          <span className="text-sm text-muted-foreground">Color</span>
          <button
            type="button"
            ref={colorTriggerRef}
            disabled={isSaving}
            aria-expanded={colorOpen}
            aria-haspopup="dialog"
            aria-label="Color"
            onClick={() => setColorOpen((open) => !open)}
            className={fieldTriggerClassName}
          >
            <span className="flex min-w-0 items-center gap-2">
              <span
                className="size-5 shrink-0 rounded-full border border-border/70"
                style={{ backgroundColor: form.color || "transparent" }}
                aria-hidden="true"
              />
              <span className="truncate text-foreground">
                {form.color ? form.color.toUpperCase() : "Select"}
              </span>
            </span>
            <ChevronDown
              className="size-4 shrink-0 text-muted-foreground"
              aria-hidden="true"
            />
          </button>
        </div>

        <div className="block">
          <span className="text-sm text-muted-foreground">Icon</span>
          <button
            type="button"
            ref={iconTriggerRef}
            disabled={isSaving}
            aria-expanded={iconOpen}
            aria-haspopup="dialog"
            aria-label="Icon"
            onClick={() => setIconOpen((open) => !open)}
            className={fieldTriggerClassName}
          >
            <span
              className={cn(
                "min-w-0 truncate",
                iconGlyph
                  ? "text-base text-foreground"
                  : "text-muted-foreground",
              )}
            >
              {iconGlyph || "Select"}
            </span>
            <ChevronDown
              className="size-4 shrink-0 text-muted-foreground"
              aria-hidden="true"
            />
          </button>
        </div>

        {error ? <Alert variant="destructive">{error}</Alert> : null}

        <div className="flex gap-2">
          <Button
            type="submit"
            disabled={isSaving || !form.subGroupId || !form.name.trim()}
            size="lg"
          >
            {isSaving
              ? "Saving"
              : editingCategory
                ? "Save changes"
                : "Create category"}
          </Button>

          {editingCategory ? (
            <Button
              type="button"
              variant="outline"
              size="lg"
              onClick={onCancel}
            >
              Cancel
            </Button>
          ) : null}
        </div>
      </div>

      <ChoiceSurface
        open={colorOpen}
        title="Choose Color"
        triggerRef={colorTriggerRef}
        onClose={() => setColorOpen(false)}
        minWidth={colorPickerPopoverSize.minWidth}
        maxHeight={colorPickerPopoverSize.maxHeight}
        fixedWidth
      >
        <ColorPicker
          value={form.color}
          disabled={isSaving}
          onChange={(color) => onFormChange({ ...form, color })}
          onCommit={() => setColorOpen(false)}
        />
      </ChoiceSurface>

      <ChoiceSurface
        open={iconOpen}
        title="Choose Icon"
        triggerRef={iconTriggerRef}
        onClose={() => setIconOpen(false)}
        minWidth={emojiPickerPopoverSize.minWidth}
        maxHeight={emojiPickerPopoverSize.maxHeight}
        fixedWidth
      >
        <EmojiPicker
          value={form.icon}
          disabled={isSaving}
          onChange={(icon) => {
            onFormChange({ ...form, icon });
            setIconOpen(false);
          }}
        />
      </ChoiceSurface>
    </Form>
  );
}

/**
 * Emoji shown on the icon control.
 * An empty icon stays blank. A stored slug uses the same emoji as the category list.
 */
function iconOnTheControl(icon: string) {
  const trimmed = icon.trim();
  if (!trimmed) {
    return "";
  }

  return getCategoryEmoji({ key: null, icon: trimmed, name: "" });
}
