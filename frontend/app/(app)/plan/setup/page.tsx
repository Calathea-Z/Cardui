import { auth } from "@clerk/nextjs/server";
import Link from "next/link";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { PageHeader } from "@/components/navigation/page-header";
import { buttonVariants } from "@/components/ui/button";
import { PlanSetupClient } from "@/features/setup";
import { loadPlanSetup } from "@/features/setup/server/loadPlanSetup";
import { cn } from "@/lib/utils";

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
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
        <Link
          href="/plan"
          className={cn(
            buttonVariants({ variant: "outline" }),
            "min-h-11 w-fit bg-card px-4",
          )}
        >
          Back to Plan
        </Link>
        <PageHeader
          title="Plan setup"
          description="These are the facts Plan already uses. Edit a group when something has changed."
        />
        <PlanSetupClient input={page.data} currency={page.data.currency} />
      </section>
    </>
  );
}
