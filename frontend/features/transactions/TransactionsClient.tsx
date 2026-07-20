"use client";

import { useMemo } from "react";
import type {
  AccountDto,
  CategoryDto,
  PagedResultDto,
  TransactionDto,
} from "@/lib/api/types";
import { TransactionDateGroup } from "./TransactionDateGroup";
import { TransactionsEmptyState } from "./TransactionsEmptyState";
import { TransactionsErrorBanner } from "./TransactionsErrorBanner";
import { TransactionsFilters } from "./TransactionsFilters";
import { TransactionsPagination } from "./TransactionsPagination";
import { groupTransactionsByDate } from "./transactionGrouping";
import { useTransactionsPage } from "./useTransactionsPage";
import { useTransactionsQueryState } from "./useTransactionsQueryState";

type TransactionsClientProps = {
  initialTransactionsPage: PagedResultDto<TransactionDto>;
  categories: CategoryDto[];
  accounts: AccountDto[];
  pageSize: number;
};

export function TransactionsClient({
  initialTransactionsPage,
  categories,
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
  } = useTransactionsPage({
    initialTransactionsPage,
    pageSize,
    search: query.search,
    accountId: query.accountId,
    categoryId: query.categoryId,
    pendingFilter: query.pendingFilter,
  });

  const transactionsByDate = useMemo(
    () => groupTransactionsByDate(transactions),
    [transactions],
  );

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
          <TransactionsErrorBanner message={errorMessage} />
        ) : null}

        <div className="app-panel overflow-hidden">
          <div>
            {transactionsByDate.map((group) => (
              <TransactionDateGroup key={group.date} group={group} />
            ))}

            {transactions.length === 0 ? (
              <TransactionsEmptyState
                trimmedSearch={query.trimmedSearch}
                hasActiveFilters={query.hasActiveFilters}
              />
            ) : null}
          </div>

          {transactionsPage.totalPages > 1 ? (
            <TransactionsPagination
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
    </main>
  );
}
