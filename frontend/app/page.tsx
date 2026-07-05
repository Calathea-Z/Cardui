import {getDashboardSummary} from "@/lib/api/dashboard";

export const dynamic = "force-dynamic";

function formatCurrency(value: number){
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(value);
}

export default async function Home() {
  const summary = await getDashboardSummary();
  
  return (
      <>
        <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
          <div>
            <p className="text-sm text-slate-400">Overview</p>
            <h1 className="text-3xl font-semibold">Dashboard</h1>
          </div>

          <div className="grid gap-4 md:grid-cols-5">
            <div className="rounded-lg border border-slate-800 bg-slate-900 p-4">
              <p className="text-sm text-slate-400">Net Worth</p>
              <p className="mt-2 text-2xl font-semibold">
                {formatCurrency(summary.netWorth)}
              </p>
            </div>

            <div className="rounded-lg border border-slate-800 bg-slate-900 p-4">
              <p className="text-sm text-slate-400">Cash</p>
              <p className="mt-2 text-2xl font-semibold">
                {formatCurrency(summary.cashBalance)}
              </p>
            </div>

            <div className="rounded-lg border border-slate-800 bg-slate-900 p-4">
              <p className="text-sm text-slate-400">Credit Cards</p>
              <p className="mt-2 text-2xl font-semibold">
                {formatCurrency(summary.creditCardBalance)}
              </p>
            </div>

            <div className="rounded-lg border border-slate-800 bg-slate-900 p-4">
              <p className="text-sm text-slate-400">Income</p>
              <p className="mt-2 text-2xl font-semibold text-emerald-400">
                {formatCurrency(summary.monthlyIncome)}
              </p>
            </div>

            <div className="rounded-lg border border-slate-800 bg-slate-900 p-4">
              <p className="text-sm text-slate-400">Spending</p>
              <p className="mt-2 text-2xl font-semibold text-rose-400">
                {formatCurrency(summary.monthlySpending)}
              </p>
            </div>
          </div>

          <div className="grid gap-4 lg:grid-cols-[1.4fr_1fr]">
            <section className="rounded-lg border border-slate-800 bg-slate-900">
              <div className="border-b border-slate-800 p-4">
                <h2 className="font-semibold">Recent Transactions</h2>
              </div>

              <div className="divide-y divide-slate-800">
                {summary.recentTransactions.map((transaction) => (
                    <div
                        key={transaction.id}
                        className="flex items-center justify-between gap-4 p-4"
                    >
                      <div>
                        <p className="font-medium">{transaction.name}</p>
                        <p className="text-sm text-slate-400">
                          {transaction.account.name}
                          {transaction.category
                              ? ` · ${transaction.category.name}`
                              : " · Uncategorized"}
                        </p>
                      </div>

                      <p className="font-medium">
                        {formatCurrency(transaction.amount)}
                      </p>
                    </div>
                ))}
              </div>
            </section>

            <section className="rounded-lg border border-slate-800 bg-slate-900">
              <div className="border-b border-slate-800 p-4">
                <h2 className="font-semibold">Spending by Category</h2>
              </div>

              <div className="space-y-4 p-4">
                {summary.spendingByCategory.map((category) => (
                    <div key={category.categoryId ?? category.categoryName}>
                      <div className="flex justify-between text-sm">
                        <span>{category.categoryName}</span>
                        <span>{formatCurrency(category.amount)}</span>
                      </div>
                      <div className="mt-2 h-2 rounded-full bg-slate-800">
                        <div
                            className="h-2 rounded-full bg-emerald-400"
                            style={{
                              width: `${Math.min(
                                  100,
                                  (category.amount / summary.monthlySpending) * 100,
                              )}%`,
                            }}
                        />
                      </div>
                    </div>
                ))}
              </div>
            </section>
          </div>
        </section>
      </>
  )
}
