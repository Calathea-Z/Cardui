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

/**
 * Account and category line under a transaction title.
 * A missing category is shown as Uncategorized.
 */
export function transactionListContext(
  transaction: Pick<TransactionDto, "account" | "category">,
) {
  const categoryName = transaction.category?.name?.trim() || "Uncategorized";
  return `${transaction.account.name} · ${categoryName}`;
}
