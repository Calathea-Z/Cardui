import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { PageHeader } from "@/components/navigation/page-header";
import { PlanReturn } from "@/components/navigation/plan-return";
import { PlanSetupClient } from "@/features/setup";
import { loadPlanSetup } from "@/features/setup/server/loadPlanSetup";

/**
 * Renders Plan setup on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Plan setup route.
 * Shows the saved summary for each existing editor and links back from those editors.
 */
export default async function PlanSetupPage() {
  await auth.protect();

  const page = await loadPlanSetup();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
        <PlanReturn />
        <PageHeader
          title="Plan setup"
          description="These are the facts Plan already uses. Edit a group when something has changed."
        />
        <PlanSetupClient input={page.data} currency={page.data.currency} />
      </section>
    </>
  );
}
