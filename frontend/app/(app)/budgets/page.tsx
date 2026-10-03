import { EmptyState } from "@/components/ui/empty-state";

export default function BudgetsPage() {
  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <div>
        <p className="text-[11px] font-medium tracking-[0.18em] text-primary uppercase">
          Planning
        </p>
        <h1 className="font-brand mt-1 text-[2.15rem] leading-none tracking-tight text-foreground">
          <span className="ink-underline">Budgets</span>
        </h1>
      </div>

      <EmptyState
        title="Budget tracking is coming soon"
        description="Set spending limits and monitor progress across categories."
        className="app-panel bg-card/50"
      />
    </section>
  );
}
