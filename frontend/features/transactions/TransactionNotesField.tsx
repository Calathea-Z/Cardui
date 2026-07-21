"use client";

import { cn } from "@/lib/utils";

type TransactionNotesFieldProps = {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
};

const MAX_NOTES_LENGTH = 1000;

export function TransactionNotesField({
  value,
  onChange,
  disabled = false,
}: TransactionNotesFieldProps) {
  return (
    <label
      className={cn(
        "flex min-h-12 items-center gap-3",
        disabled && "opacity-50",
      )}
    >
      <span className="shrink-0 text-sm font-medium text-foreground">
        Notes
      </span>
      <input
        type="text"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        maxLength={MAX_NOTES_LENGTH}
        disabled={disabled}
        placeholder="Add a note"
        className={cn(
          "min-w-0 flex-1 border-0 bg-transparent py-0 text-right text-sm text-muted-foreground outline-none",
          "placeholder:text-muted-foreground/70",
          "disabled:cursor-not-allowed",
        )}
        aria-label="Notes"
      />
    </label>
  );
}
