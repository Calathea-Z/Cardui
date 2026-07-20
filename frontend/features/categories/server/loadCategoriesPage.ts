import {
  emptyCategories,
  firstApiError,
  getCategories,
  safeApiCall,
  type CategoryDto,
} from "@/lib/api";
import type { PageLoadState } from "@/lib/pageLoadState";

export type CategoriesPageData = {
  initialCategories: CategoryDto[];
};

export async function loadCategoriesPage(): Promise<
  PageLoadState<CategoriesPageData>
> {
  const categoriesResult = await safeApiCall(getCategories, emptyCategories());

  return {
    data: {
      initialCategories: categoriesResult.data,
    },
    error: firstApiError(categoriesResult),
  };
}
