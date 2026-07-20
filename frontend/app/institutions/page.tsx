import { InstitutionsPageClient } from "@/features/institutions/InstitutionsPageClient";
import { loadInstitutionsPage } from "@/features/institutions/server/loadInstitutionsPage";

export const dynamic = "force-dynamic";

export default async function InstitutionsPage() {
  const data = await loadInstitutionsPage();

  return <InstitutionsPageClient {...data} />;
}
