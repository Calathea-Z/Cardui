import { getCategories, getTransactions } from "@/lib/api";
import { TransactionsClient } from "@/features/transactions/TransactionsClient";

export const revalidate = 30;

const DEFAULT_PAGE_SIZE = 50;

export default async function TransactionsPage() {
  const [transactionsPage, categories] = await Promise.all([
    getTransactions({ page: 1, pageSize: DEFAULT_PAGE_SIZE }),
    getCategories(),
  ]);

  return (
    <TransactionsClient
      initialTransactionsPage={transactionsPage}
      categories={categories}
      pageSize={DEFAULT_PAGE_SIZE}
    />
  );
}
