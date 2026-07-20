import { PageApiErrorBanner } from "@/components/PageApiErrorBanner";
import { DashboardView } from "@/features/dashboard/DashboardView";
import { loadDashboardPage } from "@/features/dashboard/server/loadDashboardPage";

export const dynamic = "force-dynamic";

export default async function Home() {
  const page = await loadDashboardPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <DashboardView {...page.data} />
    </>
  );
}
