"use client";

import { cn } from "@/lib/utils";

const COLOR_SWATCHES = [
  "#22c55e",
  "#14b8a6",
  "#06b6d4",
  "#3b82f6",
  "#6366f1",
  "#a855f7",
  "#ec4899",
  "#f43f5e",
  "#f97316",
  "#eab308",
  "#84cc16",
  "#64748b",
] as const;

type ColorPickerProps = {
  value: string;
  onChange: (color: string) => void;
  disabled?: boolean;
};

/**
 * Lets the user pick a preset swatch or a custom color.
 * The matching swatch is marked selected.
 */
export function ColorPicker({
  value,
  onChange,
  disabled = false,
}: ColorPickerProps) {
  const normalized = value.trim().toLowerCase();

  return (
    <div className="flex flex-col gap-3">
      <div
        role="listbox"
        aria-label="Choose a color"
        className="grid grid-cols-6 gap-2 sm:grid-cols-8"
      >
        {COLOR_SWATCHES.map((color) => {
          const selected = normalized === color.toLowerCase();

          return (
            <button
              key={color}
              type="button"
              role="option"
              aria-selected={selected}
              disabled={disabled}
              onClick={() => onChange(color)}
              className={cn(
                "size-11 rounded-lg border border-border/70 transition-transform",
                selected &&
                  "ring-2 ring-primary ring-offset-2 ring-offset-background",
                disabled && "opacity-50",
              )}
              style={{ backgroundColor: color }}
            >
              <span className="sr-only">Select {color}</span>
            </button>
          );
        })}
      </div>

      <label className="flex items-center gap-3">
        <span
          className="size-11 shrink-0 overflow-hidden rounded-lg border border-border/70"
          style={{ backgroundColor: value || "transparent" }}
        >
          <input
            type="color"
            value={value || "#22c55e"}
            onChange={(event) => onChange(event.target.value)}
            disabled={disabled}
            className="size-full cursor-pointer appearance-none border-0 bg-transparent p-0 disabled:cursor-not-allowed"
            aria-label="Custom color"
          />
        </span>
        <span className="text-sm text-muted-foreground">
          {value ? value.toUpperCase() : "Pick a custom color"}
        </span>
      </label>
    </div>
  );
}
