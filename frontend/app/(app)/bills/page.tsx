import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { BillsPageClient } from "@/features/bills";
import { loadBillsPage } from "@/features/bills/server/loadBillsPage";

/**
 * Renders the bills page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Bills route.
 * Loads bills and open accounts on the server and shows a banner when that load fails.
 */
export default async function BillsPage() {
  await auth.protect();

  const page = await loadBillsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-6 py-8">
        <BillsPageClient {...page.data} />
      </section>
    </>
  );
}
