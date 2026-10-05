import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { IncomePageClient } from "@/features/income";
import { loadIncomePage } from "@/features/income/server/loadIncomePage";

/**
 * Renders the income page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Income route.
 * Loads income sources and contributors on the server and shows a banner when that load fails.
 */
export default async function IncomePage() {
  await auth.protect();

  const page = await loadIncomePage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-6 py-8">
        <IncomePageClient {...page.data} />
      </section>
    </>
  );
}
