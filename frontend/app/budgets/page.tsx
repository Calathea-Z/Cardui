export default function BudgetsPage() {
  return (
    <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
      <div>
        <p className="text-sm text-slate-400">Planning</p>
        <h1 className="text-2xl font-semibold">Budgets</h1>
      </div>

      <div className="rounded-lg border border-slate-800 bg-slate-900/50 px-6 py-12 text-center">
        <p className="text-slate-300">Budget tracking is coming soon.</p>
        <p className="mt-2 text-sm text-slate-500">
          Set spending limits and monitor progress across categories.
        </p>
      </div>
    </section>
  );
}
