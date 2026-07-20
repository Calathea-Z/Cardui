import { PageApiErrorBanner } from "@/components/PageApiErrorBanner";
import { AccountsPageClient } from "@/features/accounts/AccountsPageClient";
import { loadAccountsPage } from "@/features/accounts/server/loadAccountsPage";

export const dynamic = "force-dynamic";

export default async function AccountsPage() {
  const page = await loadAccountsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
        <AccountsPageClient {...page.data} />
      </section>
    </>
  );
}
