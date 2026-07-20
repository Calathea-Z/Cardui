import { DashboardView } from "@/features/dashboard/DashboardView";
import {
  emptyAccountSummary,
  emptyDashboardSummary,
  getAccountsSummary,
  getDashboardSummary,
  safeApiCall,
} from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function Home() {
  const [dashboardResult, accountsResult] = await Promise.all([
    safeApiCall(getDashboardSummary, emptyDashboardSummary()),
    safeApiCall(getAccountsSummary, emptyAccountSummary()),
  ]);

  return (
    <DashboardView
      dashboardSummary={dashboardResult.data}
      accountsSummary={accountsResult.data}
    />
  );
}
