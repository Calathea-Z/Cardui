import { getCategories } from "@/lib/api";
import { CategoriesClient } from "@/features/categories/CategoriesClient";

export const revalidate = 30;

export default async function CategoriesPage() {
  const categories = await getCategories();

  return <CategoriesClient initialCategories={categories} />;
}