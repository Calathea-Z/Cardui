"use client";

import { useMemo, useState } from "react";
import { Search } from "lucide-react";
import { Alert } from "@/components/ui/alert";
import { EmptyState } from "@/components/ui/empty-state";
import { Pagination } from "@/components/ui/pagination";
import type {
  AccountDto,
  CategoryDto,
  GroupDto,
  PagedResultDto,
  SubGroupDto,
  TransactionDto,
} from "@/lib/api/types";
import { TransactionDateGroup } from "./TransactionDateGroup";
import { TransactionDetailDrawer } from "./TransactionDetailDrawer";
import { TransactionsFilters } from "./TransactionsFilters";
import { groupTransactionsByDate } from "./transactionGrouping";
import { useTransactionsPage } from "./useTransactionsPage";
import { useTransactionsQueryState } from "./useTransactionsQueryState";

type TransactionsClientProps = {
  initialTransactionsPage: PagedResultDto<TransactionDto>;
  categories: CategoryDto[];
  groups: GroupDto[];
  subGroups: SubGroupDto[];
  accounts: AccountDto[];
  pageSize: number;
};

export function TransactionsClient({
  initialTransactionsPage,
  categories: initialCategories,
  groups: initialGroups,
  subGroups: initialSubGroups,
  accounts,
  pageSize,
}: TransactionsClientProps) {
  const query = useTransactionsQueryState();
  const {
    transactionsPage,
    transactions,
    isLoading,
    errorMessage,
    loadPage,
    patchTransaction,
  } = useTransactionsPage({
    initialTransactionsPage,
    pageSize,
    search: query.search,
    accountId: query.accountId,
    categoryId: query.categoryId,
    pendingFilter: query.pendingFilter,
  });

  const [categories, setCategories] = useState(initialCategories);
  const [groups] = useState(initialGroups);
  const [subGroups] = useState(initialSubGroups);
  const [selectedTransaction, setSelectedTransaction] =
    useState<TransactionDto | null>(null);

  const transactionsByDate = useMemo(
    () => groupTransactionsByDate(transactions),
    [transactions],
  );

  function handleCategoryCreated(category: CategoryDto) {
    setCategories((current) => {
      if (current.some((item) => item.id === category.id)) {
        return current;
      }

      return [...current, category];
    });
  }

  function handleSaved(updated: TransactionDto) {
    patchTransaction(updated);
    setSelectedTransaction(updated);
  }

  return (
    <main className="min-h-screen bg-background text-foreground">
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-4 px-6 py-8">
        <TransactionsFilters
          query={query}
          accounts={accounts}
          categories={categories}
          isLoading={isLoading}
        />

        {errorMessage ? (
          <Alert variant="panel">{errorMessage}</Alert>
        ) : null}

        <div className="app-panel !overflow-visible">
          <div>
            {transactionsByDate.map((group) => (
              <TransactionDateGroup
                key={group.date}
                group={group}
                onSelectTransaction={setSelectedTransaction}
              />
            ))}

            {transactions.length === 0 ? (
              <EmptyState
                icon={<Search className="size-12 text-muted-foreground/70" />}
                title="No transactions found"
                description={
                  query.trimmedSearch
                    ? `We couldn't find any transactions matching your search of "${query.trimmedSearch}".`
                    : query.hasActiveFilters
                      ? "Try adjusting your filters."
                      : "We couldn't find any transactions."
                }
              />
            ) : null}
          </div>

          {transactionsPage.totalPages > 1 ? (
            <Pagination
              page={transactionsPage.page}
              totalPages={transactionsPage.totalPages}
              hasPreviousPage={transactionsPage.hasPreviousPage}
              hasNextPage={transactionsPage.hasNextPage}
              isLoading={isLoading}
              onPrevious={() => void loadPage(transactionsPage.page - 1)}
              onNext={() => void loadPage(transactionsPage.page + 1)}
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
    </main>
  );
}
