import { PageApiErrorBanner } from "@/components/PageApiErrorBanner";
import { CategoriesClient } from "@/features/categories";
import { loadCategoriesPage } from "@/features/categories/server/loadCategoriesPage";

export const dynamic = "force-dynamic";

export default async function CategoriesPage() {
  const page = await loadCategoriesPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <CategoriesClient {...page.data} />
    </>
  );
}
