import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { AccountsPageClient } from "@/features/accounts/AccountsPageClient";
import { loadAccountsPage } from "@/features/accounts/server/loadAccountsPage";

/**
 * Renders the accounts page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Accounts route.
 * Loads accounts on the server and shows a banner when that load fails.
 */
export default async function AccountsPage() {
  await auth.protect();

  const page = await loadAccountsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
        <AccountsPageClient {...page.data} />
      </section>
    </>
  );
}
