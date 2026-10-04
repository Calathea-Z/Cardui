import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { TransactionsClient } from "@/features/transactions";
import { loadTransactionsPage } from "@/features/transactions/server/loadTransactionsPage";

/**
 * Renders the transactions page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Transactions route.
 * Loads transactions on the server and shows a banner when that load fails.
 */
export default async function TransactionsPage() {
  await auth.protect();

  const page = await loadTransactionsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <TransactionsClient {...page.data} />
    </>
  );
}
