import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { InstitutionsPageClient } from "@/features/institutions/InstitutionsPageClient";
import { loadInstitutionsPage } from "@/features/institutions/server/loadInstitutionsPage";

/**
 * Renders the Connections page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Connections route.
 * Loads linked banks on the server and shows a banner when that load fails.
 */
export default async function ConnectionsPage() {
  await auth.protect();

  const page = await loadInstitutionsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <InstitutionsPageClient {...page.data} />
    </>
  );
}
