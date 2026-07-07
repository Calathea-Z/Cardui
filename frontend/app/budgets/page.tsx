export default function BudgetsPage() {
  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <div>
        <p className="text-sm text-muted-foreground">Planning</p>
        <h1 className="text-2xl font-semibold text-violet-50">Budgets</h1>
      </div>

      <div className="app-panel bg-card/50 px-6 py-12 text-center">
        <p className="text-foreground/90">Budget tracking is coming soon.</p>
        <p className="mt-2 text-sm text-muted-foreground">
          Set spending limits and monitor progress across categories.
        </p>
      </div>
    </section>
  );
}
