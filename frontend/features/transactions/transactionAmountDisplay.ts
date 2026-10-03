import type { TransactionDto } from "@/lib/api/types";

const TRANSFERS_CATEGORY_KEY = "transfers";

export function isBalanceReconciliation(transaction: TransactionDto) {
  return transaction.provenance === "BalanceReconciliation";
}

export function isTransferTransaction(transaction: TransactionDto) {
  const key = transaction.category?.key?.toLowerCase();
  if (key === TRANSFERS_CATEGORY_KEY) {
    return true;
  }

  // Fallback when category key isn't present on older payloads.
  const name = transaction.category?.name?.toLowerCase() ?? "";
  return name.includes("transfer");
}

function formatAbsoluteCurrency(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(Math.abs(value));
}

export type TransactionAmountDisplay = {
  kind: "income" | "spend" | "transfer";
  label: string;
};

/** Display rules: income is +green, spend is unsigned, transfers are neutral moves. */
export function getTransactionAmountDisplay(
  transaction: TransactionDto,
): TransactionAmountDisplay {
  if (isTransferTransaction(transaction) || isBalanceReconciliation(transaction)) {
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

export function sumNonTransferAmounts(transactions: TransactionDto[]) {
  return transactions
    .filter(
      (transaction) =>
        !isTransferTransaction(transaction) &&
        !isBalanceReconciliation(transaction),
    )
    .reduce((total, transaction) => total + transaction.amount, 0);
}

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
