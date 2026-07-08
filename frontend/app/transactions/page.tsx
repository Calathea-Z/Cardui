import { getTransactions } from "@/lib/api";
import { TransactionsClient } from "@/features/transactions/TransactionsClient";

export const revalidate = 30;

const DEFAULT_PAGE_SIZE = 50;

export default async function TransactionsPage() {
  const transactionsPage = await getTransactions({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });

  return (
    <TransactionsClient
      initialTransactionsPage={transactionsPage}
      pageSize={DEFAULT_PAGE_SIZE}
    />
  );
}
