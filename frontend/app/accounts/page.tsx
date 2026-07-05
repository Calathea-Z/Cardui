import { AccountsView } from "@/features/accounts/accountsView";
import { getAccounts } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function AccountsPage() {
  const accounts = await getAccounts();

  return <AccountsView accounts={accounts} />;
}
