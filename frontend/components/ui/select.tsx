"use client";

import { Check, ChevronDown } from "lucide-react";
import { useEffect, useRef, useState, useSyncExternalStore } from "react";
import { createPortal } from "react-dom";
import { AnchoredPopover } from "@/components/ui/anchored-popover";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { useDesktopChoiceList } from "@/components/ui/use-desktop-choice-list";
import { cn } from "@/lib/utils";

export type SelectOption = {
  value: string;
  label: string;
};

type SelectProps = {
  value: string;
  options: SelectOption[];
  onChange: (value: string) => void;
  title: string;
  placeholder?: string;
  disabled?: boolean;
  /** `row` matches transaction detail rows; `field` matches form controls. */
  variant?: "row" | "field";
  label?: string;
  overlayClassName?: string;
  className?: string;
  onOpenChange?: (open: boolean) => void;
};

/**
 * Choice list for a form or a detail row.
 * Under 768px it opens in a bottom sheet. From 768px up it opens in a popover anchored to the trigger. The list is portaled to document.body after the client mounts.
 * Opening the list focuses the first option, and Tab moves through the rest.
 */
export function Select({
  value,
  options,
  onChange,
  title,
  placeholder = "Select",
  disabled = false,
  variant = "field",
  label,
  overlayClassName,
  className,
  onOpenChange,
}: SelectProps) {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const optionsRef = useRef<HTMLDivElement>(null);
  const desktop = useDesktopChoiceList();
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false,
  );
  const selected = options.find((option) => option.value === value);
  const displayValue = selected?.label ?? placeholder;

  useEffect(() => {
    if (!open || desktop) {
      return;
    }

    // The phone sheet focuses its panel. Put focus on the first option after that so Tab walks the list.
    optionsRef.current?.querySelector<HTMLElement>("[role='option']")?.focus();
  }, [open, desktop]);

  function setOpenState(next: boolean) {
    setOpen(next);
    onOpenChange?.(next);
  }

  /**
   * Applies the chosen value and closes the list.
   */
  function handleSelect(nextValue: string) {
    onChange(nextValue);
    setOpenState(false);
  }

  const triggerClassName =
    variant === "row"
      ? cn(
          "flex min-h-12 w-full items-center gap-3 text-left",
          disabled && "opacity-50",
          className,
        )
      : cn(
          "flex h-10 w-full min-w-0 items-center justify-between gap-2 rounded-lg border border-border bg-card px-2.5 text-left text-sm transition-colors outline-none",
          "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
          "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
          className,
        );

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        disabled={disabled}
        onClick={() => setOpenState(!open)}
        aria-label={variant === "field" ? title : undefined}
        aria-expanded={open}
        aria-haspopup="listbox"
        className={triggerClassName}
      >
        {variant === "row" && label ? (
          <span className="shrink-0 text-sm font-medium text-foreground">
            {label}
          </span>
        ) : null}
        <span
          className={cn(
            "flex min-w-0 items-center gap-1.5",
            variant === "row"
              ? "flex-1 justify-end text-sm text-muted-foreground"
              : "flex-1 justify-between",
          )}
        >
          <span
            className={cn(
              "min-w-0 truncate",
              variant === "field" &&
                (selected ? "text-foreground" : "text-muted-foreground"),
            )}
          >
            {displayValue}
          </span>
          <ChevronDown
            className={cn(
              "size-4 shrink-0",
              variant === "field" && "text-muted-foreground",
            )}
            aria-hidden="true"
          />
        </span>
      </button>

      {mounted && desktop ? (
        <AnchoredPopover
          open={open}
          label={title}
          role="listbox"
          triggerRef={triggerRef}
          className={cn("py-1", overlayClassName)}
          onClose={() => setOpenState(false)}
          focusSelected
        >
          <SelectOptions
            options={options}
            value={value}
            layout="popover"
            containerRef={optionsRef}
            onSelect={handleSelect}
          />
        </AnchoredPopover>
      ) : null}

      {mounted && !desktop
        ? createPortal(
            <BottomSheet
              open={open}
              onClose={() => setOpenState(false)}
              title={title}
              headerAction="close"
              overlayClassName={cn("z-[110]", overlayClassName)}
              className="z-[110] max-h-[70vh]"
            >
              <SelectOptions
                options={options}
                value={value}
                layout="sheet"
                containerRef={optionsRef}
                onSelect={handleSelect}
              />
            </BottomSheet>,
            document.body,
          )
        : null}
    </>
  );
}

type SelectOptionsProps = {
  options: SelectOption[];
  value: string;
  layout: "sheet" | "popover";
  containerRef: { current: HTMLDivElement | null };
  onSelect: (value: string) => void;
};

/**
 * Renders one choice per option and marks the current value.
 * The sheet layout uses full-width rows. The popover layout uses a shorter row with its own padding.
 */
function SelectOptions({
  options,
  value,
  layout,
  containerRef,
  onSelect,
}: SelectOptionsProps) {
  return (
    <div
      ref={containerRef}
      className={
        layout === "sheet"
          ? "divide-y divide-border/70 border-y border-border/70"
          : "flex flex-col"
      }
    >
      {options.map((option) => {
        const isSelected = option.value === value;

        return (
          <button
            key={option.value || "__empty__"}
            type="button"
            role="option"
            aria-selected={isSelected}
            onClick={() => onSelect(option.value)}
            className={cn(
              "flex w-full items-center gap-3 text-left",
              layout === "sheet"
                ? "min-h-12 py-3"
                : "min-h-10 rounded-md px-3 py-2",
              "hover:bg-muted focus-visible:bg-muted focus-visible:outline-none",
            )}
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
