import { getCategories } from "@/lib/api";
import { CategoriesClient } from "@/features/categories/CategoriesClient";

export const dynamic = "force-dynamic";

export default async function CategoriesPage() {
  const categories = await getCategories();

  return <CategoriesClient initialCategories={categories} />;
}