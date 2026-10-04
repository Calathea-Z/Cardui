import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { InstitutionsPageClient } from "@/features/institutions/InstitutionsPageClient";
import { loadInstitutionsPage } from "@/features/institutions/server/loadInstitutionsPage";

export const dynamic = "force-dynamic";

export default async function InstitutionsPage() {
  await auth.protect();

  const page = await loadInstitutionsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <InstitutionsPageClient {...page.data} />
    </>
  );
}
