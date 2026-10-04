import { formatCurrency } from "@/features/accounts/formatCurrency";

/**
 * Writes a preview amount as money in or money out.
 * A missing amount is blank, a negative amount reads as money in, and zero or a positive amount reads as money out.
 */
export function formatImportAmount(
  amount: number | null,
  currency: string | null,
) {
  if (amount === null) {
    return "";
  }

  const formatted = formatCurrency(Math.abs(amount), currency);
  return amount < 0 ? `Money in ${formatted}` : `Money out ${formatted}`;
}

/**
 * Writes an import batch's created date as a short US date.
 * An unreadable value is blank.
 */
export function formatBatchDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return date.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}

/**
 * Writes a row count as "transaction" or "transactions".
 */
export function transactionLabel(count: number) {
  return `${count} transaction${count === 1 ? "" : "s"}`;
}
