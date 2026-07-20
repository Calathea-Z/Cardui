import { AccountsPageClient } from "@/features/accounts/AccountsPageClient";
import {
  emptyAccountSummary,
  emptyPlaidItems,
  getAccountsSummary,
  getPlaidItems,
  safeApiCall,
} from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function AccountsPage() {
  const [summaryResult, plaidItemsResult] = await Promise.all([
    safeApiCall(getAccountsSummary, emptyAccountSummary()),
    safeApiCall(getPlaidItems, emptyPlaidItems()),
  ]);

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <AccountsPageClient
        summary={summaryResult.data}
        plaidItems={plaidItemsResult.data}
      />
    </section>
  );
}
