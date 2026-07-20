import {
  emptyCategories,
  getCategories,
  safeApiCall,
  type CategoryDto,
} from "@/lib/api";

export type CategoriesPageData = {
  initialCategories: CategoryDto[];
};

export async function loadCategoriesPage(): Promise<CategoriesPageData> {
  const categoriesResult = await safeApiCall(getCategories, emptyCategories());

  return {
    initialCategories: categoriesResult.data,
  };
}
