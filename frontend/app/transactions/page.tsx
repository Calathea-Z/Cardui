import { getAccounts, getCategories, getTransactions } from "@/lib/api";
import { TransactionsClient } from "@/features/transactions/TransactionsClient";

export const dynamic = "force-dynamic";

const DEFAULT_PAGE_SIZE = 50;

export default async function TransactionsPage() {
  const [transactionsPage, categories, accounts] = await Promise.all([
    getTransactions({
      page: 1,
      pageSize: DEFAULT_PAGE_SIZE,
    }),
    getCategories(),
    getAccounts(),
  ]);

  return (
    <TransactionsClient
      initialTransactionsPage={transactionsPage}
      categories={categories}
      accounts={accounts}
      pageSize={DEFAULT_PAGE_SIZE}
    />
  );
}
