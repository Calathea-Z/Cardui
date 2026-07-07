import { AccountsPageClient } from "@/features/accounts/AccountsPageClient";
import { getAccountsSummary, getPlaidItems } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function AccountsPage() {
  const [summary, plaidItems] = await Promise.all([
    getAccountsSummary(),
    getPlaidItems(),
  ]);

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <AccountsPageClient summary={summary} plaidItems={plaidItems} />
    </section>
  );
}
