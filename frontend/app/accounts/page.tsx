import { AccountsPageClient } from "@/features/accounts/AccountsPageClient";
import { loadAccountsPage } from "@/features/accounts/server/loadAccountsPage";

export const dynamic = "force-dynamic";

export default async function AccountsPage() {
  const data = await loadAccountsPage();

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <AccountsPageClient {...data} />
    </section>
  );
}
