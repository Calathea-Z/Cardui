import {
  emptyCategories,
  emptyGroups,
  emptySubGroups,
  firstApiError,
  getCategories,
  getGroups,
  getSubGroups,
  safeApiCall,
} from "@/lib/api/server";
import type { CategoryDto, GroupDto, SubGroupDto } from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

export type CategoriesPageData = {
  initialCategories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
};

export async function loadCategoriesPage(): Promise<
  PageLoadState<CategoriesPageData>
> {
  const [categoriesResult, groupsResult, subGroupsResult] = await Promise.all([
    safeApiCall(getCategories, emptyCategories()),
    safeApiCall(getGroups, emptyGroups()),
    safeApiCall(() => getSubGroups(), emptySubGroups()),
  ]);

  return {
    data: {
      initialCategories: categoriesResult.data,
      groups: groupsResult.data,
      subGroups: subGroupsResult.data,
    },
    error: firstApiError(categoriesResult, groupsResult, subGroupsResult),
  };
}
