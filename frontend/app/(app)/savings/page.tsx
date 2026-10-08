import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { SavingsPageClient } from "@/features/savings";
import { loadSavingsPage } from "@/features/savings/server/loadSavingsPage";

/**
 * Renders the savings page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Savings route.
 * Loads goals and cash accounts on the server and shows a banner when that load fails.
 */
export default async function SavingsPage() {
  await auth.protect();

  const page = await loadSavingsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-6 py-8">
        <SavingsPageClient {...page.data} />
      </section>
    </>
  );
}
