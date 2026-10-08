/**
 * Reads the extra-each-month field.
 * Blank is zero, the minimums-only plan. A negative or non-numeric amount is null so the field can ask again.
 * The amount is rounded to cents before it is sent.
 */
export function parsePlanExtra(value: string) {
  const trimmed = value.trim();
  if (!trimmed) {
    return 0;
  }

  const parsed = Number(trimmed);
  if (!Number.isFinite(parsed) || parsed < 0) {
    return null;
  }

  return Math.round(parsed * 100) / 100;
}
