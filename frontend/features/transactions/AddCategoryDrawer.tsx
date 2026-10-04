"use client";

import { Check, ChevronDown } from "lucide-react";
import { useId, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { ColorPicker } from "@/features/categories/ColorPicker";
import { EmojiPicker } from "@/features/categories/EmojiPicker";
import { createCategory } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { CategoryDto, GroupDto, SubGroupDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";

type AddCategoryDrawerProps = {
  open: boolean;
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  initialSubGroupId?: string;
  onClose: () => void;
  onCreated: (category: CategoryDto) => void;
};

type FormState = {
  name: string;
  emoji: string;
  color: string;
  groupId: string;
  subGroupId: string;
};

type PickerKind = "emoji" | "color" | "group" | "subgroup" | null;

/**
 * Orders items by sort order, then by name.
 * The input list is left unchanged.
 */
function sortByOrderThenName<T extends { sortOrder: number; name: string }>(
  items: T[],
) {
  return items
    .slice()
    .sort(
      (left, right) =>
        left.sortOrder - right.sortOrder || left.name.localeCompare(right.name),
    );
}

/**
 * Builds a blank category form.
 * A known starting subgroup also selects its group.
 */
function createInitialForm(
  initialSubGroupId: string,
  subGroups: SubGroupDto[],
): FormState {
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
 * Opens the add-category sheet.
 * Each time the sheet opens, the form remounts so the previous draft is dropped.
 */
export function AddCategoryDrawer({
  open,
  groups,
  subGroups,
  initialSubGroupId = "",
  onClose,
  onCreated,
}: AddCategoryDrawerProps) {
  const [session, setSession] = useState(0);
  const [wasOpen, setWasOpen] = useState(open);

  if (open !== wasOpen) {
    setWasOpen(open);
    if (open) {
      setSession((current) => current + 1);
    }
  }

  return (
    <AddCategoryDrawerSession
      key={session}
      open={open}
      groups={groups}
      subGroups={subGroups}
      initialSubGroupId={initialSubGroupId}
      onClose={onClose}
      onCreated={onCreated}
    />
  );
}

/**
 * Collects a name, emoji, color, group, and subgroup, then creates the category.
 * Save stays off until every field is filled and the form differs from its start, and choosing a group clears the subgroup.
 */
function AddCategoryDrawerSession({
  open,
  groups,
  subGroups,
  initialSubGroupId = "",
  onClose,
  onCreated,
}: AddCategoryDrawerProps) {
  const formId = useId();
  const initialForm = createInitialForm(initialSubGroupId, subGroups);
  const [form, setForm] = useState<FormState>(initialForm);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [openPicker, setOpenPicker] = useState<PickerKind>(null);

  const sortedGroups = sortByOrderThenName(groups);
  const visibleSubGroups = sortByOrderThenName(
    subGroups.filter((subGroup) => subGroup.groupId === form.groupId),
  );

  const effectiveSubGroupId =
    form.subGroupId &&
    visibleSubGroups.some((subGroup) => subGroup.id === form.subGroupId)
      ? form.subGroupId
      : "";

  const selectedGroupName =
    sortedGroups.find((group) => group.id === form.groupId)?.name ?? "Select";
  const selectedSubGroupName =
    visibleSubGroups.find((subGroup) => subGroup.id === effectiveSubGroupId)
      ?.name ?? "Select";

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
   * Closes an open picker and then the form.
   */
  function handleClose() {
    setOpenPicker(null);
    onClose();
  }

  /**
   * Closes the sheet the household is looking at.
   * An open emoji, color, group, or subgroup picker closes first and leaves the form open.
   */
  function handleSheetClose() {
    if (openPicker) {
      setOpenPicker(null);
      return;
    }

    handleClose();
  }

  /**
   * Creates the category when the form is complete and has been changed.
   * The name is trimmed, and the subgroup must still belong to the chosen group.
   */
  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

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

  return (
    <>
      <BottomSheet
        open={open}
        onClose={handleSheetClose}
        title="Add Category"
        headerAction="close-leading"
        closeOnEscape={openPicker === null}
        overlayClassName="z-80"
        className={FULL_SCREEN_SHEET_CLASSNAME}
        headerTrailing={
          <Button
            type="submit"
            form={formId}
            variant="ghost"
            disabled={!canSave}
            className="h-9 px-2 text-sm font-semibold text-transfer disabled:text-muted-foreground"
          >
            {isSaving ? "Saving" : "Save"}
          </Button>
        }
      >
        <form
          id={formId}
          onSubmit={handleSubmit}
          className="flex flex-col gap-4"
        >
          <div className="divide-y divide-border/70 border-y border-border/70">
            <label
              className={cn(
                "flex min-h-12 items-center gap-3",
                isSaving && "opacity-50",
              )}
            >
              <span className="shrink-0 text-sm font-medium text-foreground">
                Name
              </span>
              <input
                type="text"
                value={form.name}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    name: event.target.value,
                  }))
                }
                disabled={isSaving}
                placeholder="Pets"
                autoFocus
                required
                className={cn(
                  "min-w-0 flex-1 border-0 bg-transparent py-0 text-right text-sm text-muted-foreground outline-none",
                  "placeholder:text-muted-foreground/70",
                  "disabled:cursor-not-allowed",
                )}
                aria-label="Name"
              />
            </label>

            <button
              type="button"
              disabled={isSaving}
              onClick={() => setOpenPicker("emoji")}
              className={cn(
                "flex min-h-12 w-full items-center gap-3 text-left",
                isSaving && "opacity-50",
              )}
            >
              <span className="shrink-0 text-sm font-medium text-foreground">
                Emoji
              </span>
              <span className="flex min-w-0 flex-1 items-center justify-end gap-1.5 text-sm text-muted-foreground">
                <span className="truncate text-base">
                  {form.emoji || "Select"}
                </span>
                <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
              </span>
            </button>

            <button
              type="button"
              disabled={isSaving}
              onClick={() => setOpenPicker("color")}
              className={cn(
                "flex min-h-12 w-full items-center gap-3 text-left",
                isSaving && "opacity-50",
              )}
            >
              <span className="shrink-0 text-sm font-medium text-foreground">
                Color
              </span>
              <span className="flex min-w-0 flex-1 items-center justify-end gap-2 text-sm text-muted-foreground">
                {form.color ? (
                  <span
                    className="size-5 shrink-0 rounded-full border border-border/70"
                    style={{ backgroundColor: form.color }}
                    aria-hidden="true"
                  />
                ) : null}
                <span className="truncate">
                  {form.color ? form.color.toUpperCase() : "Select"}
                </span>
                <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
              </span>
            </button>

            <button
              type="button"
              disabled={isSaving}
              onClick={() => setOpenPicker("group")}
              className={cn(
                "flex min-h-12 w-full items-center gap-3 text-left",
                isSaving && "opacity-50",
              )}
            >
              <span className="shrink-0 text-sm font-medium text-foreground">
                Group
              </span>
              <span className="flex min-w-0 flex-1 items-center justify-end gap-1.5 text-sm text-muted-foreground">
                <span className="truncate">{selectedGroupName}</span>
                <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
              </span>
            </button>

            <button
              type="button"
              disabled={isSaving || !form.groupId}
              onClick={() => setOpenPicker("subgroup")}
              className={cn(
                "flex min-h-12 w-full items-center gap-3 text-left",
                (isSaving || !form.groupId) && "opacity-50",
              )}
            >
              <span className="shrink-0 text-sm font-medium text-foreground">
                Sub-group
              </span>
              <span className="flex min-w-0 flex-1 items-center justify-end gap-1.5 text-sm text-muted-foreground">
                <span className="truncate">{selectedSubGroupName}</span>
                <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
              </span>
            </button>
          </div>

          {error ? <Alert variant="destructive">{error}</Alert> : null}
        </form>
      </BottomSheet>

      <BottomSheet
        open={openPicker === "emoji"}
        onClose={() => setOpenPicker(null)}
        title="Choose Emoji"
        headerAction="close"
        overlayClassName="z-90"
        className="max-h-[70vh]"
      >
        <EmojiPicker
          value={form.emoji}
          onChange={(emoji) => {
            setForm((current) => ({ ...current, emoji }));
            setOpenPicker(null);
          }}
        />
      </BottomSheet>

      <BottomSheet
        open={openPicker === "color"}
        onClose={() => setOpenPicker(null)}
        title="Choose Color"
        headerAction="close"
        overlayClassName="z-90"
        className="max-h-[70vh]"
      >
        <ColorPicker
          value={form.color}
          onChange={(color) => {
            setForm((current) => ({ ...current, color }));
            setOpenPicker(null);
          }}
        />
      </BottomSheet>

      <BottomSheet
        open={openPicker === "group"}
        onClose={() => setOpenPicker(null)}
        title="Group"
        headerAction="close"
        overlayClassName="z-90"
        className="max-h-[70vh]"
      >
        <OptionList
          options={sortedGroups.map((group) => ({
            value: group.id,
            label: group.name,
          }))}
          value={form.groupId}
          onChange={(groupId) => {
            setForm((current) => ({
              ...current,
              groupId,
              subGroupId: "",
            }));
            setOpenPicker(null);
          }}
        />
      </BottomSheet>

      <BottomSheet
        open={openPicker === "subgroup"}
        onClose={() => setOpenPicker(null)}
        title="Sub-group"
        headerAction="close"
        overlayClassName="z-90"
        className="max-h-[70vh]"
      >
        <OptionList
          options={visibleSubGroups.map((subGroup) => ({
            value: subGroup.id,
            label: subGroup.name,
          }))}
          value={effectiveSubGroupId}
          onChange={(subGroupId) => {
            setForm((current) => ({ ...current, subGroupId }));
            setOpenPicker(null);
          }}
        />
      </BottomSheet>
    </>
  );
}

/**
 * Shows a single-choice list and marks the current value.
 * An empty list tells the household that no options are available.
 */
function OptionList({
  options,
  value,
  onChange,
}: {
  options: { value: string; label: string }[];
  value: string;
  onChange: (value: string) => void;
}) {
  if (options.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">No options available.</p>
    );
  }

  return (
    <div className="divide-y divide-border/70 border-y border-border/70">
      {options.map((option) => {
        const isSelected = option.value === value;

        return (
          <button
            key={option.value}
            type="button"
            onClick={() => onChange(option.value)}
            className="flex min-h-12 w-full items-center gap-3 py-3 text-left"
          >
            <span className="min-w-0 flex-1 truncate text-sm font-medium text-foreground">
              {option.label}
            </span>
            {isSelected ? (
              <Check
                className="size-4 shrink-0 text-primary"
                aria-hidden="true"
              />
            ) : null}
          </button>
        );
      })}
    </div>
  );
}
