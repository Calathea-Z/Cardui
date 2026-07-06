import { AccountsView } from "@/features/accounts/AccountsView";
import { getAccountsSummary, getPlaidItems } from "@/lib/api";
import { ConnectedInstitutionsPanel } from "@/features/plaid/ConnectedInstitutionsPanel";

export const dynamic = "force-dynamic";

export default async function AccountsPage() {
  const [summary, plaidItems] = await Promise.all([
    getAccountsSummary(),
    getPlaidItems(),
  ]);

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <ConnectedInstitutionsPanel initialItems={plaidItems} />
      <AccountsView summary={summary} />
    </section>
  );
}
