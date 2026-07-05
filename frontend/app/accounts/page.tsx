import { AccountsView } from "@/features/accounts/accountsView";
import { getAccounts } from "@/lib/api/account";

export default async function AccountsPage() {
  const accounts = await getAccounts();

  return <AccountsView accounts={accounts} />;
}
