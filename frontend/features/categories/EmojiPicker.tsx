"use client";

import { cn } from "@/lib/utils";

const CATEGORY_EMOJI_OPTIONS = [
  "💰",
  "💵",
  "💳",
  "🏦",
  "📊",
  "🍽️",
  "🛒",
  "☕",
  "🍕",
  "🚗",
  "⛽",
  "🚌",
  "✈️",
  "🏠",
  "🔑",
  "🧾",
  "💡",
  "📱",
  "🛍️",
  "👕",
  "🎁",
  "🎬",
  "🎮",
  "🎵",
  "💊",
  "🏋️",
  "🧒",
  "📚",
  "💼",
  "🐕",
  "✂️",
  "🔧",
  "📦",
  "↔️",
  "⭐",
  "❤️",
] as const;

type EmojiPickerProps = {
  value: string;
  onChange: (emoji: string) => void;
  disabled?: boolean;
};

/**
 * Lets the user pick a category emoji from the preset list.
 * The current emoji is marked selected.
 */
export function EmojiPicker({
  value,
  onChange,
  disabled = false,
}: EmojiPickerProps) {
  return (
    <div
      role="listbox"
      aria-label="Choose an emoji"
      className="grid grid-cols-6 gap-2 sm:grid-cols-8"
    >
      {CATEGORY_EMOJI_OPTIONS.map((emoji) => {
        const selected = value === emoji;

        return (
          <button
            key={emoji}
            type="button"
            role="option"
            aria-selected={selected}
            disabled={disabled}
            onClick={() => onChange(emoji)}
            className={cn(
              "flex size-11 items-center justify-center rounded-lg text-xl transition-colors",
              "border border-border/70 bg-card/40 hover:bg-accent",
              selected && "border-primary bg-accent ring-2 ring-primary/40",
              disabled && "opacity-50",
            )}
          >
            <span aria-hidden="true">{emoji}</span>
            <span className="sr-only">Select {emoji}</span>
          </button>
        );
      })}
    </div>
  );
}
