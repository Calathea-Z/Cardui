import { PageApiErrorBanner } from "@/components/PageApiErrorBanner";
import { TransactionsClient } from "@/features/transactions";
import { loadTransactionsPage } from "@/features/transactions/server/loadTransactionsPage";

export const dynamic = "force-dynamic";

export default async function TransactionsPage() {
  const page = await loadTransactionsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <TransactionsClient {...page.data} />
    </>
  );
}
