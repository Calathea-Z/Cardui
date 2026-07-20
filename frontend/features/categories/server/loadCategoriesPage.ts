import {
  emptyCategories,
  firstApiError,
  getCategories,
  safeApiCall,
} from "@/lib/api/server";
import type { CategoryDto } from "@/lib/api/types";
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
