"use client";

import { cn } from "@/lib/utils";

type SegmentedControlOption<T extends string> = {
  value: T;
  label: string;
};

type SegmentedControlProps<T extends string> = {
  label: string;
  options: SegmentedControlOption<T>[];
  value: T;
  onChange: (value: T) => void;
  className?: string;
};

/**
 * A row of pressed buttons that picks one view.
 * The label names the group. The pressed option is marked with `aria-pressed` and a raised white segment, so it is not color alone.
 * Each segment is 44px tall on a phone and 36px from 768px up.
 */
export function SegmentedControl<T extends string>({
  label,
  options,
  value,
  onChange,
  className,
}: SegmentedControlProps<T>) {
  return (
    <div
      role="group"
      aria-label={label}
      className={cn(
        "flex w-full rounded-lg border border-border bg-muted p-0.5 sm:inline-flex sm:w-auto",
        className,
      )}
    >
      {options.map((option) => {
        const pressed = option.value === value;
        return (
          <button
            key={option.value}
            type="button"
            aria-pressed={pressed}
            onClick={() => onChange(option.value)}
            className={cn(
              "min-h-11 flex-1 rounded-md px-3 text-sm font-medium whitespace-nowrap transition-colors motion-reduce:transition-none md:min-h-9",
              "focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none",
              pressed
                ? "border border-border bg-card text-foreground shadow-xs"
                : "border border-transparent text-muted-foreground hover:text-foreground",
            )}
          >
            {option.label}
          </button>
        );
      })}
    </div>
  );
}
