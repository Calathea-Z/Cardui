import { DashboardView } from "@/features/dashboard/DashboardView";
import { loadDashboardPage } from "@/features/dashboard/server/loadDashboardPage";

export const dynamic = "force-dynamic";

export default async function Home() {
  const data = await loadDashboardPage();

  return <DashboardView {...data} />;
}
