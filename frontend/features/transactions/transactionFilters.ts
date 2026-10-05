/**
 * Counts the account, category, and status filters.
 * Search is left out because that field stays on the screen.
 */
export function countTransactionChoiceFilters(filters: {
  accountId: string;
  categoryId: string;
  pendingFilter: string;
}) {
  let count = 0;

  if (filters.accountId !== "") {
    count += 1;
  }

  if (filters.categoryId !== "") {
    count += 1;
  }

  if (filters.pendingFilter !== "all") {
    count += 1;
  }

  return count;
}
