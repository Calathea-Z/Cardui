import { getDayTotalDisplay } from "./transactionAmountDisplay";
import {
  formatDateSectionHeader,
  type TransactionDateGroup as TransactionDateGroupModel,
} from "./transactionGrouping";
import { TransactionRow } from "./TransactionRow";

type TransactionDateGroupProps = {
  group: TransactionDateGroupModel;
};

export function TransactionDateGroup({ group }: TransactionDateGroupProps) {
  const dayTotal = getDayTotalDisplay(group.transactions);

  return (
    <section className="border-b border-border/80 last:border-b-0">
      <div className="sticky top-[calc(4.5rem+env(safe-area-inset-top))] z-10 flex items-center justify-between gap-4 border-y border-primary/20 bg-panel-header px-4 py-3 md:top-0">
        <h2 className="text-sm font-semibold tracking-wide text-foreground">
          {formatDateSectionHeader(group.date)}
        </h2>
        <p
          className={
            dayTotal.kind === "income"
              ? "ledger-amount shrink-0 text-sm text-success"
              : "ledger-amount shrink-0 text-sm text-foreground"
          }
        >
          {dayTotal.label}
        </p>
      </div>

      <div className="divide-y divide-border/70 bg-background/30">
        {group.transactions.map((transaction) => (
          <TransactionRow key={transaction.id} transaction={transaction} />
        ))}
      </div>
    </section>
  );
}
