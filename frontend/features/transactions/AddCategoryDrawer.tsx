"use client";

import { Check, ChevronDown } from "lucide-react";
import { useId, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { ColorPicker } from "@/features/categories/ColorPicker";
import { EmojiPicker } from "@/features/categories/EmojiPicker";
import type { CategoryDto, GroupDto, SubGroupDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { useAddCategoryDrawer } from "./useAddCategoryDrawer";

type AddCategoryDrawerProps = {
  open: boolean;
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  initialSubGroupId?: string;
  onClose: () => void;
  onCreated: (category: CategoryDto) => void;
};

type PickerKind = "emoji" | "color" | "group" | "subgroup" | null;

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
  const draft = useAddCategoryDrawer({
    groups,
    subGroups,
    initialSubGroupId,
    onCreated,
  });
  const [openPicker, setOpenPicker] = useState<PickerKind>(null);

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
   * Creates the category from the draft.
   * The draft ignores a save that is still incomplete.
   */
  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void draft.submit();
  }

  return (
    <>
      <BottomSheet
        open={open}
        onClose={handleSheetClose}
        title="Add Category"
        headerAction="close-leading"
        closeOnEscape={openPicker === null}
        presentation="panel"
        overlayClassName="z-[130]"
        headerTrailing={
          <Button
            type="submit"
            form={formId}
            variant="ghost"
            disabled={!draft.canSave}
            className="h-9 px-2 text-sm font-semibold text-transfer disabled:text-muted-foreground"
          >
            {draft.isSaving ? "Saving" : "Save"}
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
                draft.isSaving && "opacity-50",
              )}
            >
              <span className="shrink-0 text-sm font-medium text-foreground">
                Name
              </span>
              <input
                type="text"
                value={draft.form.name}
                onChange={(event) =>
                  draft.setForm((current) => ({
                    ...current,
                    name: event.target.value,
                  }))
                }
                disabled={draft.isSaving}
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
              disabled={draft.isSaving}
              onClick={() => setOpenPicker("emoji")}
              className={cn(
                "flex min-h-12 w-full items-center gap-3 text-left",
                draft.isSaving && "opacity-50",
              )}
            >
              <span className="shrink-0 text-sm font-medium text-foreground">
                Emoji
              </span>
              <span className="flex min-w-0 flex-1 items-center justify-end gap-1.5 text-sm text-muted-foreground">
                <span className="truncate text-base">
                  {draft.form.emoji || "Select"}
                </span>
                <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
              </span>
            </button>

            <button
              type="button"
              disabled={draft.isSaving}
              onClick={() => setOpenPicker("color")}
              className={cn(
                "flex min-h-12 w-full items-center gap-3 text-left",
                draft.isSaving && "opacity-50",
              )}
            >
              <span className="shrink-0 text-sm font-medium text-foreground">
                Color
              </span>
              <span className="flex min-w-0 flex-1 items-center justify-end gap-2 text-sm text-muted-foreground">
                {draft.form.color ? (
                  <span
                    className="size-5 shrink-0 rounded-full border border-border/70"
                    style={{ backgroundColor: draft.form.color }}
                    aria-hidden="true"
                  />
                ) : null}
                <span className="truncate">
                  {draft.form.color ? draft.form.color.toUpperCase() : "Select"}
                </span>
                <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
              </span>
            </button>

            <button
              type="button"
              disabled={draft.isSaving}
              onClick={() => setOpenPicker("group")}
              className={cn(
                "flex min-h-12 w-full items-center gap-3 text-left",
                draft.isSaving && "opacity-50",
              )}
            >
              <span className="shrink-0 text-sm font-medium text-foreground">
                Group
              </span>
              <span className="flex min-w-0 flex-1 items-center justify-end gap-1.5 text-sm text-muted-foreground">
                <span className="truncate">{draft.selectedGroupName}</span>
                <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
              </span>
            </button>

            <button
              type="button"
              disabled={draft.isSaving || !draft.form.groupId}
              onClick={() => setOpenPicker("subgroup")}
              className={cn(
                "flex min-h-12 w-full items-center gap-3 text-left",
                (draft.isSaving || !draft.form.groupId) && "opacity-50",
              )}
            >
              <span className="shrink-0 text-sm font-medium text-foreground">
                Sub-group
              </span>
              <span className="flex min-w-0 flex-1 items-center justify-end gap-1.5 text-sm text-muted-foreground">
                <span className="truncate">{draft.selectedSubGroupName}</span>
                <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
              </span>
            </button>
          </div>

          {draft.error ? (
            <Alert variant="destructive">{draft.error}</Alert>
          ) : null}
        </form>
      </BottomSheet>

      <BottomSheet
        open={openPicker === "emoji"}
        onClose={() => setOpenPicker(null)}
        title="Choose Emoji"
        headerAction="close"
        overlayClassName="z-[140]"
        className="max-h-[70vh]"
      >
        <EmojiPicker
          value={draft.form.emoji}
          onChange={(emoji) => {
            draft.setForm((current) => ({ ...current, emoji }));
            setOpenPicker(null);
          }}
        />
      </BottomSheet>

      <BottomSheet
        open={openPicker === "color"}
        onClose={() => setOpenPicker(null)}
        title="Choose Color"
        headerAction="close"
        overlayClassName="z-[140]"
        className="max-h-[70vh]"
      >
        <ColorPicker
          value={draft.form.color}
          onChange={(color) => {
            draft.setForm((current) => ({ ...current, color }));
            setOpenPicker(null);
          }}
        />
      </BottomSheet>

      <BottomSheet
        open={openPicker === "group"}
        onClose={() => setOpenPicker(null)}
        title="Group"
        headerAction="close"
        overlayClassName="z-[140]"
        className="max-h-[70vh]"
      >
        <OptionList
          options={draft.sortedGroups.map((group) => ({
            value: group.id,
            label: group.name,
          }))}
          value={draft.form.groupId}
          onChange={(groupId) => {
            draft.setForm((current) => ({
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
        overlayClassName="z-[140]"
        className="max-h-[70vh]"
      >
        <OptionList
          options={draft.visibleSubGroups.map((subGroup) => ({
            value: subGroup.id,
            label: subGroup.name,
          }))}
          value={draft.effectiveSubGroupId}
          onChange={(subGroupId) => {
            draft.setForm((current) => ({ ...current, subGroupId }));
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
