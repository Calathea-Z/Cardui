import type { TransactionDto } from "@/lib/api";
import { getCategoryEmoji } from "@/features/categories/categoryEmoji";
import {
  getTransactionAmountDisplay,
  isTransferTransaction,
} from "@/features/transactions/transactionAmountDisplay";
import { cn } from "@/lib/utils";

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
        {transactions.map((transaction) => {
          const amount = getTransactionAmountDisplay(transaction);
          const isTransfer = isTransferTransaction(transaction);

          return (
            <div
              key={transaction.id}
              className="flex items-center justify-between gap-4 p-4 transition hover:bg-accent/20"
            >
              <div className="flex min-w-0 items-start gap-3">
                <span
                  className="mt-0.5 shrink-0 text-base leading-none"
                  aria-hidden="true"
                >
                  {getCategoryEmoji(transaction.category)}
                </span>
                <div className="min-w-0">
                  <p className="truncate font-medium">{transaction.name}</p>
                  {isTransfer ? (
                    <p className="mt-0.5 text-xs text-transfer">
                      Move between accounts
                    </p>
                  ) : null}
                </div>
              </div>

              <div className="shrink-0 text-right">
                <p
                  className={cn(
                    "font-medium tabular-nums",
                    amount.kind === "income" && "text-success",
                    amount.kind === "transfer" && "text-transfer",
                  )}
                >
                  {amount.label}
                </p>
              </div>
            </div>
          );
        })}

        {transactions.length === 0 ? (
          <div className="p-6 text-center text-sm text-muted-foreground">
            Transactions will appear here after your first account sync.
          </div>
        ) : null}
      </div>
    </section>
  );
}
