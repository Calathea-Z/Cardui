import { AccountsView } from "@/features/accounts/AccountsView";
import { getAccounts, getPlaidItems } from "@/lib/api";
import { ConnectedInstitutionsPanel } from "@/features/plaid/ConnectedInstitutionsPanel";
export const dynamic = "force-dynamic";

export default async function AccountsPage() {
  const [accounts, plaidItems] = await Promise.all([
    getAccounts(),
    getPlaidItems(),
  ]);

  return (
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
        <ConnectedInstitutionsPanel initialItems={plaidItems} />
        <AccountsView accounts={accounts} />
      </section>
  );
}
