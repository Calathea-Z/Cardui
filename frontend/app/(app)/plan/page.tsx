import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { PlanPageClient } from "@/features/plan";
import { loadPlanPage } from "@/features/plan/server/loadPlanPage";

/**
 * Renders the plan page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Plan route.
 * Loads the cash-flow recovery report and shows a banner when that load fails.
 */
export default async function PlanPage() {
  await auth.protect();

  const page = await loadPlanPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
        <PlanPageClient report={page.data} failed={page.error !== null} />
      </section>
    </>
  );
}
