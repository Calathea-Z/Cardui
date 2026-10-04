import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { DashboardView } from "@/features/dashboard/DashboardView";
import { loadDashboardPage } from "@/features/dashboard/server/loadDashboardPage";

/**
 * Renders the dashboard on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Dashboard route.
 * Loads the dashboard on the server and shows a banner when that load fails.
 */
export default async function Home() {
  await auth.protect();

  const page = await loadDashboardPage();

  return (
    <>
      {page.error ? <PageApiErrorBanner message={page.error} /> : null}
      <DashboardView {...page.data} />
    </>
  );
}
