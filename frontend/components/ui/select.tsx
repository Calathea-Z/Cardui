"use client";

import { Check, ChevronDown } from "lucide-react";
import { useState, useSyncExternalStore } from "react";
import { createPortal } from "react-dom";
import { BottomSheet } from "@/components/ui/bottom-sheet";
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
  const mounted = useSyncExternalStore(
    () => () => {},
    () => true,
    () => false,
  );
  const selected = options.find((option) => option.value === value);
  const displayValue = selected?.label ?? placeholder;

  function setOpenState(next: boolean) {
    setOpen(next);
    onOpenChange?.(next);
  }

  function handleSelect(nextValue: string) {
    onChange(nextValue);
    setOpenState(false);
  }

  return (
    <>
      {variant === "row" ? (
        <button
          type="button"
          disabled={disabled}
          onClick={() => setOpenState(true)}
          className={cn(
            "flex min-h-12 w-full items-center gap-3 text-left",
            disabled && "opacity-50",
            className,
          )}
        >
          {label ? (
            <span className="shrink-0 text-sm font-medium text-foreground">
              {label}
            </span>
          ) : null}
          <span className="flex min-w-0 flex-1 items-center justify-end gap-1.5 text-sm text-muted-foreground">
            <span className="truncate">{displayValue}</span>
            <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
          </span>
        </button>
      ) : (
        <button
          type="button"
          disabled={disabled}
          onClick={() => setOpenState(true)}
          aria-label={title}
          className={cn(
            "flex h-10 w-full min-w-0 items-center justify-between gap-2 rounded-lg border border-input bg-transparent px-2.5 text-left text-sm transition-colors outline-none",
            "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
            "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
            className,
          )}
        >
          <span
            className={cn(
              "min-w-0 truncate",
              selected ? "text-foreground" : "text-muted-foreground",
            )}
          >
            {displayValue}
          </span>
          <ChevronDown
            className="size-4 shrink-0 text-muted-foreground"
            aria-hidden="true"
          />
        </button>
      )}

      {mounted
        ? createPortal(
            <BottomSheet
              open={open}
              onClose={() => setOpenState(false)}
              title={title}
              headerAction="close"
              overlayClassName={cn("z-[70]", overlayClassName)}
              className="z-[70] max-h-[70vh]"
            >
              <div className="divide-y divide-border/70 border-y border-border/70">
                {options.map((option) => {
                  const isSelected = option.value === value;

                  return (
                    <button
                      key={option.value || "__empty__"}
                      type="button"
                      onClick={() => handleSelect(option.value)}
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
            </BottomSheet>,
            document.body,
          )
        : null}
    </>
  );
}
