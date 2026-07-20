import { TransactionsClient } from "@/features/transactions";
import {
  emptyAccounts,
  emptyCategories,
  emptyPagedResult,
  getAccounts,
  getCategories,
  getTransactions,
  safeApiCall,
} from "@/lib/api";

export const dynamic = "force-dynamic";

const DEFAULT_PAGE_SIZE = 50;

export default async function TransactionsPage() {
  const [transactionsResult, categoriesResult, accountsResult] =
    await Promise.all([
      safeApiCall(
        () =>
          getTransactions({
            page: 1,
            pageSize: DEFAULT_PAGE_SIZE,
          }),
        emptyPagedResult(1, DEFAULT_PAGE_SIZE),
      ),
      safeApiCall(getCategories, emptyCategories()),
      safeApiCall(getAccounts, emptyAccounts()),
    ]);

  return (
    <TransactionsClient
      initialTransactionsPage={transactionsResult.data}
      categories={categoriesResult.data}
      accounts={accountsResult.data}
      pageSize={DEFAULT_PAGE_SIZE}
    />
  );
}
