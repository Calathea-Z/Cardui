import {
  emptyAccountSummary,
  emptyCategories,
  emptyDashboardSummary,
  emptyGroups,
  emptySubGroups,
  firstApiError,
  getAccountsSummary,
  getCategories,
  getDashboardSummary,
  getGroups,
  getSubGroups,
  safeApiCall,
} from "@/lib/api/server";
import type {
  AccountSummaryDto,
  CategoryDto,
  DashboardSummaryDto,
  GroupDto,
  SubGroupDto,
} from "@/lib/api/types";
import type { PageLoadState } from "@/lib/pageLoadState";

export type DashboardPageData = {
  dashboardSummary: DashboardSummaryDto;
  accountsSummary: AccountSummaryDto;
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
};

/**
 * Loads the dashboard summary, accounts, categories, groups, and subgroups together.
 * A failed call still returns empty fallback data, and the page keeps the first error.
 */
export async function loadDashboardPage(): Promise<
  PageLoadState<DashboardPageData>
> {
  const [
    dashboardResult,
    accountsResult,
    categoriesResult,
    groupsResult,
    subGroupsResult,
  ] = await Promise.all([
    safeApiCall(getDashboardSummary, emptyDashboardSummary()),
    safeApiCall(getAccountsSummary, emptyAccountSummary()),
    safeApiCall(getCategories, emptyCategories()),
    safeApiCall(getGroups, emptyGroups()),
    safeApiCall(() => getSubGroups(), emptySubGroups()),
  ]);

  return {
    data: {
      dashboardSummary: dashboardResult.data,
      accountsSummary: accountsResult.data,
      categories: categoriesResult.data,
      groups: groupsResult.data,
      subGroups: subGroupsResult.data,
    },
    error: firstApiError(
      dashboardResult,
      accountsResult,
      categoriesResult,
      groupsResult,
      subGroupsResult,
    ),
  };
}
