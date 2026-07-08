import type { TransactionDto } from "@/lib/api";
import { formatCurrency } from "@/features/accounts/formatCurrency";

type DashboardRecentTransactionsWidgetProps = {
  transactions: TransactionDto[];
};

export function DashboardRecentTransactionsWidget({
  transactions,
}: DashboardRecentTransactionsWidgetProps) {
  return (
    <section className="app-panel">
      <div className="app-panel-header p-4">
        <h2 className="app-section-title">Recent Transactions</h2>
      </div>

      <div className="divide-y divide-border/70">
        {transactions.map((transaction) => (
          <div
            key={transaction.id}
            className="flex items-center justify-between gap-4 p-4 transition hover:bg-accent/20"
          >
            <div className="min-w-0">
              <p className="truncate font-medium">{transaction.name}</p>
              <p className="text-sm text-muted-foreground">
                {transaction.account.name}
                {transaction.category
                  ? ` · ${transaction.category.name}`
                  : " · Uncategorized"}
              </p>
            </div>

            <p className="shrink-0 font-medium tabular-nums">
              {formatCurrency(transaction.amount)}
            </p>
          </div>
        ))}

        {transactions.length === 0 ? (
          <div className="p-6 text-center text-sm text-muted-foreground">
            Transactions will appear here after your first account sync.
          </div>
        ) : null}
      </div>
    </section>
  );
}
