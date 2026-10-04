import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { HouseholdPageClient } from "@/features/household/HouseholdPageClient";
import { loadHouseholdPage } from "@/features/household/server/loadHouseholdPage";

export const dynamic = "force-dynamic";

export default async function HouseholdPage() {
  await auth.protect();

  const page = await loadHouseholdPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-6 py-8">
        <HouseholdPageClient profile={page.data} />
      </section>
    </>
  );
}
