import { CategoriesClient } from "@/features/categories/CategoriesClient";
import { emptyCategories, getCategories, safeApiCall } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function CategoriesPage() {
  const categoriesResult = await safeApiCall(getCategories, emptyCategories());

  return <CategoriesClient initialCategories={categoriesResult.data} />;
}
