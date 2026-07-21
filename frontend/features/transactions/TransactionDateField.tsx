"use client";

import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

type TransactionDateFieldProps = {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
};

function formatDisplayDate(value: string) {
  if (!value) {
    return "Select date";
  }

  const [year, month, day] = value.split("-").map(Number);
  if (!year || !month || !day) {
    return value;
  }

  return new Date(year, month - 1, day).toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

export function TransactionDateField({
  value,
  onChange,
  disabled = false,
}: TransactionDateFieldProps) {
  return (
    <label
      className={cn(
        "relative flex min-h-12 items-center gap-3",
        disabled && "opacity-50",
      )}
    >
      <span className="shrink-0 text-sm font-medium text-foreground">Date</span>
      <span className="flex min-w-0 flex-1 items-center justify-end gap-1.5 text-sm text-muted-foreground">
        <span className="truncate">{formatDisplayDate(value)}</span>
        <ChevronDown className="size-4 shrink-0" aria-hidden="true" />
      </span>
      <input
        type="date"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
        required
        className="absolute inset-0 cursor-pointer opacity-0 disabled:cursor-not-allowed"
        aria-label="Date"
      />
    </label>
  );
}
