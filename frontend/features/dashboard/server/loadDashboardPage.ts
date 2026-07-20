import {
  emptyAccountSummary,
  emptyDashboardSummary,
  getAccountsSummary,
  getDashboardSummary,
  safeApiCall,
  type AccountSummaryDto,
  type DashboardSummaryDto,
} from "@/lib/api";

export type DashboardPageData = {
  dashboardSummary: DashboardSummaryDto;
  accountsSummary: AccountSummaryDto;
};

export async function loadDashboardPage(): Promise<DashboardPageData> {
  const [dashboardResult, accountsResult] = await Promise.all([
    safeApiCall(getDashboardSummary, emptyDashboardSummary()),
    safeApiCall(getAccountsSummary, emptyAccountSummary()),
  ]);

  return {
    dashboardSummary: dashboardResult.data,
    accountsSummary: accountsResult.data,
  };
}
