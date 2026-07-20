import { CategoriesClient } from "@/features/categories";
import { loadCategoriesPage } from "@/features/categories/server/loadCategoriesPage";

export const dynamic = "force-dynamic";

export default async function CategoriesPage() {
  const data = await loadCategoriesPage();

  return <CategoriesClient {...data} />;
}
