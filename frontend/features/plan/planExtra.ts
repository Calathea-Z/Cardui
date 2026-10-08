import { moneyDigits } from "@/features/accounts/formatCurrency";

/**
 * Reads the extra-each-month field.
 * Blank is zero, the minimums-only plan. A thousands comma is part of the amount.
 * A negative amount or other text is null so the field can ask again. The amount is rounded to cents before it is sent.
 */
export function parsePlanExtra(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return 0;
  }

  const digits = moneyDigits(trimmed);
  if (digits === null) {
    return null;
  }

  const parsed = Number(digits);
  if (!Number.isFinite(parsed) || parsed < 0) {
    return null;
  }

  return Math.round(parsed * 100) / 100;
}
