import { getAccountsSummary, getDashboardSummary } from "@/lib/api";
import { DashboardView } from "@/features/dashboard/DashboardView";

export const revalidate = 30;

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
