import { getCategories, getTransactions } from "@/lib/api";
import { TransactionsClient } from "../../features/transactions/transactions-client";

export const dynamic = "force-dynamic";

export default async function TransactionsPage() {
  const [transactions, categories] = await Promise.all([
    getTransactions(),
    getCategories(),
  ]);

  return (
    <TransactionsClient
      initialTransactions={transactions}
      categories={categories}
    />
  );
}
