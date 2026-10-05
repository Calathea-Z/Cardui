"use client";

import { DateField } from "@/components/ui/date-field";

type TransactionDateFieldProps = {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  onOpenChange?: (open: boolean) => void;
};

/**
 * Lets the household pick the transaction date.
 * The calendar opens in the themed sheet. The stored value is `YYYY-MM-DD`.
 */
export function TransactionDateField({
  value,
  onChange,
  disabled = false,
  onOpenChange,
}: TransactionDateFieldProps) {
  return (
    <DateField
      variant="row"
      title="Date"
      label="Date"
      value={value}
      onChange={onChange}
      disabled={disabled}
      onOpenChange={onOpenChange}
    />
  );
}
