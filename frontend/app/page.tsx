import { getAccountsSummary, getDashboardSummary } from "@/lib/api";
import { DashboardView } from "@/features/dashboard/DashboardView";

export const dynamic = "force-dynamic";

export default async function Home() {
  const [dashboardSummary, accountsSummary] = await Promise.all([
    getDashboardSummary(),
    getAccountsSummary(),
  ]);

  return (
    <DashboardView
      dashboardSummary={dashboardSummary}
      accountsSummary={accountsSummary}
    />
  );
}
