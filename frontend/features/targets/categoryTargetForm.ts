import type { CategoryTargetLineDto } from "@/lib/api/types";

/**
 * The target form.
 * Amount stays text until submit. Rollover is the choice to carry this month forward.
 */
export type CategoryTargetFormState = {
  amount: string;
  rollover: boolean;
};

/**
 * Opens the form from a category row.
 * A missing target stays blank. Zero stays zero.
 */
export function targetToForm(
  line: Pick<CategoryTargetLineDto, "target" | "rollover">,
): CategoryTargetFormState {
  return {
    amount: line.target === null ? "" : String(line.target),
    rollover: line.rollover,
  };
}

/**
 * Reads a target amount.
 * Blank is not zero. Zero is a known target. Extra fraction digits are rejected.
 */
export function readTargetAmount(
  value: string,
): { ok: true; amount: number } | { ok: false; error: string } {
  const trimmed = value.trim();
  if (!trimmed) {
    return { ok: false, error: "Enter the target as zero or more." };
  }

  if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) {
    return { ok: false, error: "Enter the target in dollars and cents." };
  }

  const amount = Number(trimmed);
  if (!Number.isFinite(amount) || amount < 0) {
    return { ok: false, error: "Enter the target as zero or more." };
  }

  if (amount > 100_000_000) {
    return { ok: false, error: "That amount is too large." };
  }

  return { ok: true, amount };
}
