"use client";

import { HexColorInput, HexColorPicker } from "react-colorful";
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

const FALLBACK_COLOR = "#22c55e";

/** Popover size for the swatches, the saturation picker, and the hex field. */
export const colorPickerPopoverSize = {
  minWidth: 336,
  maxHeight: 400,
} as const;

type ColorPickerProps = {
  value: string;
  onChange: (color: string) => void;
  /**
   * Called after a swatch is chosen.
   * The saturation picker and hex field only call onChange, so the popover can stay open while the color is adjusted.
   */
  onCommit?: () => void;
  disabled?: boolean;
};

/**
 * Lets the user pick a preset swatch or a custom color.
 * The matching swatch is marked selected. A custom color uses the themed picker instead of the browser color dialog.
 */
export function ColorPicker({
  value,
  onChange,
  onCommit,
  disabled = false,
}: ColorPickerProps) {
  const pickerColor = isSixDigitHex(value) ? value : FALLBACK_COLOR;
  const normalized = pickerColor.trim().toLowerCase();

  /**
   * Stores a swatch and tells the parent the choice is finished.
   */
  function selectSwatch(color: string) {
    onChange(color);
    onCommit?.();
  }

  return (
    <div
      className={cn(
        "flex flex-col gap-3",
        disabled && "pointer-events-none opacity-50",
      )}
    >
      <div
        role="listbox"
        aria-label="Choose a color"
        className="grid grid-cols-6 gap-2"
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
              onClick={() => selectSwatch(color)}
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

      <HexColorPicker
        className="h-40! w-full!"
        color={pickerColor}
        onChange={onChange}
      />

      <HexColorInput
        color={pickerColor}
        onChange={onChange}
        prefixed
        disabled={disabled}
        aria-label="Hex color"
        className="h-10 w-full rounded-lg border border-border bg-card px-2.5 font-mono text-sm text-foreground uppercase outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
      />
    </div>
  );
}

/**
 * Reports whether a color is a six-digit hex value.
 * The saturation picker needs that shape. Anything else falls back to the default green.
 */
function isSixDigitHex(value: string) {
  return /^#[0-9a-f]{6}$/i.test(value.trim());
}
