import { TransactionsClient } from "@/features/transactions";
import { loadTransactionsPage } from "@/features/transactions/server/loadTransactionsPage";

export const dynamic = "force-dynamic";

export default async function TransactionsPage() {
  const data = await loadTransactionsPage();

  return <TransactionsClient {...data} />;
}
