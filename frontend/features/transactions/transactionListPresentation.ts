import type { TransactionDto } from "@/lib/api/types";

/**
 * Title shown on a transaction row.
 * A merchant name is used when the bank sent one. Otherwise the stored name is used. The statement text is not parsed into a merchant.
 */
export function transactionListTitle(
  transaction: Pick<TransactionDto, "name" | "merchantName">,
) {
  const merchantName = transaction.merchantName?.trim();
  if (merchantName) {
    return merchantName;
  }

  return transaction.name;
}

export const transactionClassifications = ["Card payment", "Transfer"] as const;

export type TransactionClassification =
  (typeof transactionClassifications)[number];

/**
 * The one classification mark for a row.
 * A card payment is named once, even when its category is still a transfer or a spending category.
 */
export function transactionClassificationLabel(input: {
  cardPayment: boolean;
  transfer: boolean;
}): TransactionClassification | null {
  if (input.cardPayment) {
    return "Card payment";
  }

  if (input.transfer) {
    return "Transfer";
  }

  return null;
}

/**
 * Account and category line under a transaction title.
 * A card payment names the account only, so the spending category is not a second label. A missing category is Uncategorized.
 */
export function transactionListContext(
  transaction: Pick<TransactionDto, "account" | "category">,
  cardPayment = false,
) {
  if (cardPayment) {
    return transaction.account.name;
  }

  const categoryName = transaction.category?.name?.trim() || "Uncategorized";
  return `${transaction.account.name} · ${categoryName}`;
}
