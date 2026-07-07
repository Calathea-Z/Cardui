import { getDashboardSummary } from "@/lib/api";
import { PlaidLinkButton } from "@/features/plaid/PlaidLinkButton";

export const dynamic = "force-dynamic";

function formatCurrency(value: number) {
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
          <p className="text-sm text-muted-foreground">Overview</p>
          <h1 className="text-3xl font-semibold text-violet-50">Dashboard</h1>
          <PlaidLinkButton />
        </div>

        <div className="grid gap-4 md:grid-cols-5">
          <div className="app-panel p-4">
            <p className="text-sm text-muted-foreground">Net Worth</p>
            <p className="mt-2 text-2xl font-semibold">
              {formatCurrency(summary.netWorth)}
            </p>
          </div>

          <div className="app-panel p-4">
            <p className="text-sm text-muted-foreground">Cash</p>
            <p className="mt-2 text-2xl font-semibold">
              {formatCurrency(summary.cashBalance)}
            </p>
          </div>

          <div className="app-panel p-4">
            <p className="text-sm text-muted-foreground">Credit Cards</p>
            <p className="mt-2 text-2xl font-semibold">
              {formatCurrency(summary.creditCardBalance)}
            </p>
          </div>

          <div className="app-panel p-4">
            <p className="text-sm text-muted-foreground">Income</p>
            <p className="mt-2 text-2xl font-semibold text-success">
              {formatCurrency(summary.monthlyIncome)}
            </p>
          </div>

          <div className="app-panel p-4">
            <p className="text-sm text-muted-foreground">Spending</p>
            <p className="mt-2 text-2xl font-semibold text-destructive">
              {formatCurrency(summary.monthlySpending)}
            </p>
          </div>
        </div>

        <div className="grid gap-4 lg:grid-cols-[1.4fr_1fr]">
          <section className="app-panel">
            <div className="app-panel-header p-4">
              <h2 className="app-section-title">Recent Transactions</h2>
            </div>

            <div className="divide-y divide-border/70">
              {summary.recentTransactions.map((transaction) => (
                <div
                  key={transaction.id}
                  className="flex items-center justify-between gap-4 p-4 transition hover:bg-accent/20"
                >
                  <div>
                    <p className="font-medium">{transaction.name}</p>
                    <p className="text-sm text-muted-foreground">
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

          <section className="app-panel">
            <div className="app-panel-header p-4">
              <h2 className="app-section-title">Spending by Category</h2>
            </div>

            <div className="space-y-4 p-4">
              {summary.spendingByCategory.map((category) => (
                <div key={category.categoryId ?? category.categoryName}>
                  <div className="flex justify-between text-sm">
                    <span>{category.categoryName}</span>
                    <span>{formatCurrency(category.amount)}</span>
                  </div>
                  <div className="mt-2 h-2 rounded-full bg-muted">
                    <div
                      className="h-2 rounded-full bg-primary"
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
  );
}
