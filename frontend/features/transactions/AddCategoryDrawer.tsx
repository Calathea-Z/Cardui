"use client";

import { ChevronDown } from "lucide-react";
import { useId, useRef, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { ChoiceSurface } from "@/components/ui/choice-surface";
import { Select } from "@/components/ui/select";
import {
  ColorPicker,
  colorPickerPopoverSize,
} from "@/features/categories/ColorPicker";
import {
  EmojiPicker,
  emojiPickerPopoverSize,
} from "@/features/categories/EmojiPicker";
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
 * Save stays off until every field is filled and the form differs from its start. Choosing a group clears the subgroup. Escape closes an open picker before the form.
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
  const [emojiOpen, setEmojiOpen] = useState(false);
  const [colorOpen, setColorOpen] = useState(false);
  const [openLists, setOpenLists] = useState(0);
  const emojiTriggerRef = useRef<HTMLButtonElement>(null);
  const colorTriggerRef = useRef<HTMLButtonElement>(null);
  const pickerOpen = emojiOpen || colorOpen || openLists > 0;

  /**
   * Tracks how many group or subgroup lists are open.
   * The form stays up while that count is above zero.
   */
  function handleListOpenChange(open: boolean) {
    setOpenLists((count) => Math.max(0, count + (open ? 1 : -1)));
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
        onClose={onClose}
        title="Add Category"
        headerAction="close-leading"
        closeOnEscape={!pickerOpen}
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
              ref={emojiTriggerRef}
              disabled={draft.isSaving}
              aria-expanded={emojiOpen}
              aria-haspopup="dialog"
              onClick={() => setEmojiOpen((open) => !open)}
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
              ref={colorTriggerRef}
              disabled={draft.isSaving}
              aria-expanded={colorOpen}
              aria-haspopup="dialog"
              onClick={() => setColorOpen((open) => !open)}
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

            <Select
              variant="row"
              label="Group"
              title="Group"
              value={draft.form.groupId}
              disabled={draft.isSaving}
              placeholder="Select"
              overlayClassName="z-[140]"
              onOpenChange={handleListOpenChange}
              onChange={(groupId) => {
                draft.setForm((current) => ({
                  ...current,
                  groupId,
                  subGroupId: "",
                }));
              }}
              options={draft.sortedGroups.map((group) => ({
                value: group.id,
                label: group.name,
              }))}
            />

            <Select
              variant="row"
              label="Sub-group"
              title="Sub-group"
              value={draft.effectiveSubGroupId}
              disabled={draft.isSaving || !draft.form.groupId}
              placeholder="Select"
              overlayClassName="z-[140]"
              onOpenChange={handleListOpenChange}
              onChange={(subGroupId) => {
                draft.setForm((current) => ({ ...current, subGroupId }));
              }}
              options={draft.visibleSubGroups.map((subGroup) => ({
                value: subGroup.id,
                label: subGroup.name,
              }))}
            />
          </div>

          {draft.error ? (
            <Alert variant="destructive">{draft.error}</Alert>
          ) : null}
        </form>
      </BottomSheet>

      <ChoiceSurface
        open={emojiOpen}
        title="Choose Emoji"
        triggerRef={emojiTriggerRef}
        onClose={() => setEmojiOpen(false)}
        overlayClassName="z-[140]"
        minWidth={emojiPickerPopoverSize.minWidth}
        maxHeight={emojiPickerPopoverSize.maxHeight}
        fixedWidth
      >
        <EmojiPicker
          value={draft.form.emoji}
          onChange={(emoji) => {
            draft.setForm((current) => ({ ...current, emoji }));
            setEmojiOpen(false);
          }}
        />
      </ChoiceSurface>

      <ChoiceSurface
        open={colorOpen}
        title="Choose Color"
        triggerRef={colorTriggerRef}
        onClose={() => setColorOpen(false)}
        overlayClassName="z-[140]"
        minWidth={colorPickerPopoverSize.minWidth}
        maxHeight={colorPickerPopoverSize.maxHeight}
        fixedWidth
      >
        <ColorPicker
          value={draft.form.color}
          onChange={(color) => {
            draft.setForm((current) => ({ ...current, color }));
          }}
          onCommit={() => setColorOpen(false)}
        />
      </ChoiceSurface>
    </>
  );
}
