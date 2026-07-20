import {
  emptyAccountSummary,
  emptyDashboardSummary,
  firstApiError,
  getAccountsSummary,
  getDashboardSummary,
  safeApiCall,
  type AccountSummaryDto,
  type DashboardSummaryDto,
} from "@/lib/api";
import type { PageLoadState } from "@/lib/pageLoadState";

export type DashboardPageData = {
  dashboardSummary: DashboardSummaryDto;
  accountsSummary: AccountSummaryDto;
};

export async function loadDashboardPage(): Promise<
  PageLoadState<DashboardPageData>
> {
  const [dashboardResult, accountsResult] = await Promise.all([
    safeApiCall(getDashboardSummary, emptyDashboardSummary()),
    safeApiCall(getAccountsSummary, emptyAccountSummary()),
  ]);

  return {
    data: {
      dashboardSummary: dashboardResult.data,
      accountsSummary: accountsResult.data,
    },
    error: firstApiError(dashboardResult, accountsResult),
  };
}
