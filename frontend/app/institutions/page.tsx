import { PageApiErrorBanner } from "@/components/PageApiErrorBanner";
import { InstitutionsPageClient } from "@/features/institutions/InstitutionsPageClient";
import { loadInstitutionsPage } from "@/features/institutions/server/loadInstitutionsPage";

export const dynamic = "force-dynamic";

export default async function InstitutionsPage() {
  const page = await loadInstitutionsPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <InstitutionsPageClient {...page.data} />
    </>
  );
}
