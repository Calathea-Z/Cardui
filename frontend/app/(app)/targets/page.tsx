import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { TargetsPageClient } from "@/features/targets";
import { loadTargetsPage } from "@/features/targets/server/loadTargetsPage";

/**
 * Renders the targets page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Targets route.
 * Loads the current month of category targets and shows a banner when that load fails.
 */
export default async function TargetsPage() {
  await auth.protect();

  const page = await loadTargetsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-6 py-8">
        <TargetsPageClient {...page.data} />
      </section>
    </>
  );
}
