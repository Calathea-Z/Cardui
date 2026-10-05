import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { DebtsPageClient } from "@/features/debts";
import { loadDebtsPage } from "@/features/debts/server/loadDebtsPage";

/**
 * Renders the debts page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Debts route.
 * Loads debts and open accounts on the server and shows a banner when that load fails.
 */
export default async function DebtsPage() {
  await auth.protect();

  const page = await loadDebtsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-6 py-8">
        <DebtsPageClient {...page.data} />
      </section>
    </>
  );
}
