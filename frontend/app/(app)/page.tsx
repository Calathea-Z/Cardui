import { auth } from "@clerk/nextjs/server";
import { PageApiErrorBanner } from "@/components/page-api-error-banner";
import { DashboardView } from "@/features/dashboard/DashboardView";
import { loadDashboardPage } from "@/features/dashboard/server/loadDashboardPage";

export const dynamic = "force-dynamic";

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
