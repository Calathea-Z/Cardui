import { auth } from "@clerk/nextjs/server";
import { PlanPageClient } from "@/features/plan";

/**
 * Renders the plan page on each request.
 */
export const dynamic = "force-dynamic";

/**
 * Plan route.
 * The signed-in layout already requires a session. This page has no rows to load.
 */
export default async function PlanPage() {
  await auth.protect();

  return (
    <section className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-6 py-8">
      <PlanPageClient />
    </section>
  );
}
