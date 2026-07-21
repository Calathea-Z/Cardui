"use client";

import { useState } from "react";
import { EmptyState } from "@/components/ui/empty-state";
import { TransactionDetailDrawer } from "@/features/transactions/TransactionDetailDrawer";
import { TransactionRow } from "@/features/transactions/TransactionRow";
import type {
  CategoryDto,
  GroupDto,
  SubGroupDto,
  TransactionDto,
} from "@/lib/api/types";

type DashboardRecentTransactionsWidgetProps = {
  transactions: TransactionDto[];
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
};

export function DashboardRecentTransactionsWidget({
  transactions: initialTransactions,
  categories: initialCategories,
  groups,
  subGroups: initialSubGroups,
}: DashboardRecentTransactionsWidgetProps) {
  const [transactions, setTransactions] = useState(initialTransactions);
  const [categories, setCategories] = useState(initialCategories);
  const [subGroups] = useState(initialSubGroups);
  const [selectedTransaction, setSelectedTransaction] =
    useState<TransactionDto | null>(null);

  function handleCategoryCreated(category: CategoryDto) {
    setCategories((current) => {
      if (current.some((item) => item.id === category.id)) {
        return current;
      }

      return [...current, category];
    });
  }

  function handleSaved(updated: TransactionDto) {
    setTransactions((current) =>
      current.map((transaction) =>
        transaction.id === updated.id ? updated : transaction,
      ),
    );
    setSelectedTransaction(updated);
  }

  return (
    <>
      <section className="app-panel">
        <div className="app-panel-header p-4">
          <h2 className="app-section-title">Recent Transactions</h2>
        </div>

        <div className="divide-y divide-border/70">
          {transactions.map((transaction) => (
            <TransactionRow
              key={transaction.id}
              transaction={transaction}
              onSelect={setSelectedTransaction}
            />
          ))}

          {transactions.length === 0 ? (
            <EmptyState
              title="No transactions yet"
              description="Transactions will appear here after your first account sync."
              className="py-8 [&_p]:text-sm [&_p]:font-normal"
            />
          ) : null}
        </div>
      </section>

      <TransactionDetailDrawer
        transaction={selectedTransaction}
        categories={categories}
        groups={groups}
        subGroups={subGroups}
        onClose={() => setSelectedTransaction(null)}
        onSaved={handleSaved}
        onCategoryCreated={handleCategoryCreated}
      />
    </>
  );
}
