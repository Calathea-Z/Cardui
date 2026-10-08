"use client";

import { useMemo, useState } from "react";
import { Search } from "lucide-react";
import { PageHeader } from "@/components/navigation/page-header";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
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
import { AddTransactionSheet } from "./AddTransactionSheet";
import { ImportCsvSheet } from "./ImportCsvSheet";
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

/**
 * Renders the transaction list, filters, and the sheets for adding or importing.
 * A category created here stays on the page, and restoring a transaction reloads the first page.
 */
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
    removeTransaction,
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
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [isImportOpen, setIsImportOpen] = useState(false);

  const transactionsByDate = useMemo(
    () => groupTransactionsByDate(transactions),
    [transactions],
  );

  /**
   * Adds a category created from the detail or category drawer.
   * A category already in the list is left unchanged.
   */
  function handleCategoryCreated(category: CategoryDto) {
    setCategories((current) => {
      if (current.some((item) => item.id === category.id)) {
        return current;
      }

      return [...current, category];
    });
  }

  /**
   * Puts a saved transaction back into the list and the open detail.
   */
  function handleSaved(updated: TransactionDto) {
    patchTransaction(updated);
    setSelectedTransaction(updated);
  }

  return (
    <div className="min-h-screen bg-background text-foreground">
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-4 px-4 py-6 md:px-8 md:py-8">
        <PageHeader
          title="Activity"
          actions={
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsImportOpen(true)}
              >
                Import CSV
              </Button>
              <Button type="button" onClick={() => setIsAddOpen(true)}>
                Add transaction
              </Button>
            </div>
          }
        />

        <TransactionsFilters
          query={query}
          accounts={accounts}
          categories={categories}
          isLoading={isLoading}
        />

        {isLoading ? (
          <p
            className="text-sm text-muted-foreground"
            role="status"
            aria-live="polite"
          >
            Updating activity. The previous rows stay visible until the refresh
            finishes.
          </p>
        ) : null}

        {errorMessage ? <Alert variant="panel">{errorMessage}</Alert> : null}

        <div
          className="app-panel overflow-visible!"
          aria-busy={isLoading}
          aria-label="Activity results"
        >
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
                action={
                  !query.trimmedSearch && !query.hasActiveFilters ? (
                    <Button type="button" onClick={() => setIsAddOpen(true)}>
                      Add transaction
                    </Button>
                  ) : undefined
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
        onSelectTransaction={setSelectedTransaction}
        onArchived={(transactionId) => {
          removeTransaction(transactionId);
          setSelectedTransaction(null);
        }}
        onRestored={() => {
          setSelectedTransaction(null);
          void loadPage(1);
        }}
      />

      <AddTransactionSheet
        open={isAddOpen}
        accounts={accounts}
        categories={categories}
        onClose={() => setIsAddOpen(false)}
        onCreated={() => void loadPage(1)}
      />

      <ImportCsvSheet
        open={isImportOpen}
        accounts={accounts}
        onClose={() => setIsImportOpen(false)}
        onImported={() => void loadPage(1)}
      />
    </div>
  );
}
