import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { CategoriesClient } from "@/features/categories";
import { loadCategoriesPage } from "@/features/categories/server/loadCategoriesPage";

/**
 * Renders the categories page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Categories route.
 * Loads categories on the server and renders the category manager.
 */
export default async function CategoriesPage() {
  await auth.protect();

  const page = await loadCategoriesPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <CategoriesClient {...page.data} />
    </>
  );
}
