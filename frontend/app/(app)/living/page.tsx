import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { LivingPageClient } from "@/features/living";
import { loadLivingPage } from "@/features/living/server/loadLivingPage";

/**
 * Renders the living page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Living route.
 * Loads contribution shares and monthly living spending, and shows a banner when that load fails.
 */
export default async function LivingPage() {
  await auth.protect();

  const page = await loadLivingPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-6 py-8">
        <LivingPageClient page={page.data} />
      </section>
    </>
  );
}
