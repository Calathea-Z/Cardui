import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { AllocationPageClient } from "@/features/allocation";
import { loadAllocationPage } from "@/features/allocation/server/loadAllocationPage";

/**
 * Renders the allocation page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Allocation route.
 * Loads the saved preference and shows a banner when that load fails.
 */
export default async function AllocationPage() {
  await auth.protect();

  const page = await loadAllocationPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-6 py-8">
        <AllocationPageClient page={page.data} />
      </section>
    </>
  );
}
