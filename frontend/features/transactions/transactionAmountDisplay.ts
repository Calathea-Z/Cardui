import type { TransactionDto } from "@/lib/api/types";

const TRANSFERS_CATEGORY_KEY = "transfers";

/**
 * True when the row is a balance adjustment.
 */
export function isBalanceReconciliation(transaction: TransactionDto) {
  return transaction.provenance === "BalanceReconciliation";
}

/**
 * True when the row is a transfer.
 * Older payloads without a category key are recognized by the category name.
 */
export function isTransferTransaction(transaction: TransactionDto) {
  const key = transaction.category?.key?.toLowerCase();
  if (key === TRANSFERS_CATEGORY_KEY) {
    return true;
  }

  // Fallback when category key isn't present on older payloads.
  const name = transaction.category?.name?.toLowerCase() ?? "";
  return name.includes("transfer");
}

/**
 * Formats an amount as unsigned USD.
 * The display rules choose the sign and the color.
 */
function formatAbsoluteCurrency(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(Math.abs(value));
}

/**
 * How one amount should look.
 * Income is a positive green amount. Spending is unsigned. Transfers are neutral.
 */
export type TransactionAmountDisplay = {
  kind: "income" | "spend" | "transfer";
  label: string;
};

/**
 * Chooses the label and color kind for one transaction.
 * Transfers and balance adjustments stay unsigned so they are not counted as spending.
 */
export function getTransactionAmountDisplay(
  transaction: TransactionDto,
): TransactionAmountDisplay {
  if (
    isTransferTransaction(transaction) ||
    isBalanceReconciliation(transaction)
  ) {
    return {
      kind: "transfer",
      label: formatAbsoluteCurrency(transaction.amount),
    };
  }

  if (transaction.amount < 0) {
    return {
      kind: "income",
      label: `+${formatAbsoluteCurrency(transaction.amount)}`,
    };
  }

  return {
    kind: "spend",
    label: formatAbsoluteCurrency(transaction.amount),
  };
}

/**
 * Sums a day's amounts after leaving out transfers and balance adjustments.
 */
export function sumNonTransferAmounts(transactions: TransactionDto[]) {
  return transactions
    .filter(
      (transaction) =>
        !isTransferTransaction(transaction) &&
        !isBalanceReconciliation(transaction),
    )
    .reduce((total, transaction) => total + transaction.amount, 0);
}

/**
 * Chooses the day-header label from the non-transfer total.
 * A negative total is income. Zero and positive totals read as spending.
 */
export function getDayTotalDisplay(transactions: TransactionDto[]) {
  const total = sumNonTransferAmounts(transactions);

  if (total < 0) {
    return {
      kind: "income" as const,
      label: `+${formatAbsoluteCurrency(total)}`,
    };
  }

  return {
    kind: "spend" as const,
    label: formatAbsoluteCurrency(total),
  };
}
