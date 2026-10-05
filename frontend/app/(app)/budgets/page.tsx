import { auth } from "@clerk/nextjs/server";
import { EmptyState } from "@/components/ui/empty-state";

/**
 * Budgets route.
 * Requires a signed-in session and shows that budget tracking is coming soon.
 */
export default async function BudgetsPage() {
  await auth.protect();

  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <h1 className="text-[1.75rem] font-semibold leading-none tracking-[-0.02em] text-foreground">
        Budgets
      </h1>

      <EmptyState
        title="Budget tracking is coming soon"
        description="Set spending limits and monitor progress across categories."
        className="app-panel bg-card/50"
      />
    </section>
  );
}
