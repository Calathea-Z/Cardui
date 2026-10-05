import type { DebtKind } from "@/lib/api/types";

/**
 * Type choices in the debt form.
 * The value is the API member name. The label is the type in plain language.
 */
export const debtKindOptions: Array<{
  value: DebtKind;
  label: string;
}> = [
  { value: "Revolving", label: "Revolving" },
  { value: "Installment", label: "Installment" },
];

/**
 * Plain-language type for a stored member name.
 * An unrecognized name is shown as stored.
 */
export function debtKindLabel(kind: string) {
  return debtKindOptions.find((option) => option.value === kind)?.label ?? kind;
}

/**
 * True when the value is revolving or installment.
 */
export function isDebtKind(value: string): value is DebtKind {
  return debtKindOptions.some((option) => option.value === value);
}
