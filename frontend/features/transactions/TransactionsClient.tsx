"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { CircleX, Search } from "lucide-react";
import {
  getApiErrorMessage,
  getTransactions,
  updateTransactionCategory,
} from "@/lib/api";
import type {
  AccountDto,
  CategoryDto,
  PagedResultDto,
  TransactionDto,
} from "@/lib/api";

type TransactionsClientProps = {
  initialTransactionsPage: PagedResultDto<TransactionDto>;
  categories: CategoryDto[];
  accounts: AccountDto[];
  pageSize: number;
};

type PendingFilter = "all" | "pending" | "posted";

const STATUS_OPTIONS: { value: PendingFilter; label: string }[] = [
  { value: "all", label: "All statuses" },
  { value: "posted", label: "Posted" },
  { value: "pending", label: "Pending" },
];

function formatCurrency(value: number) {
  const isIncome = value < 0;

  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(isIncome ? Math.abs(value) : value);
}

function toDateKey(value: string) {
  return value.slice(0, 10);
}

function formatDateKey(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

function formatDateSectionHeader(dateKey: string) {
  const [year, month, day] = dateKey.split("-").map(Number);
  const date = new Date(year, month - 1, day);
  const todayKey = formatDateKey(new Date());
  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);
  const yesterdayKey = formatDateKey(yesterday);

  if (dateKey === todayKey) {
    return "Today";
  }

  if (dateKey === yesterdayKey) {
    return "Yesterday";
  }

  return new Intl.DateTimeFormat("en-US", {
    weekday: "long",
    month: "long",
    day: "numeric",
    ...(date.getFullYear() !== new Date().getFullYear()
      ? { year: "numeric" }
      : {}),
  }).format(date);
}

function groupTransactionsByDate(transactions: TransactionDto[]) {
  const groups: { date: string; transactions: TransactionDto[] }[] = [];

  for (const transaction of transactions) {
    const date = toDateKey(transaction.date);
    const lastGroup = groups.at(-1);

    if (lastGroup?.date === date) {
      lastGroup.transactions.push(transaction);
      continue;
    }

    groups.push({ date, transactions: [transaction] });
  }

  return groups;
}

function toPendingQueryValue(filter: PendingFilter) {
  if (filter === "pending") {
    return true;
  }

  if (filter === "posted") {
    return false;
  }

  return undefined;
}

export function TransactionsClient({
  initialTransactionsPage,
  categories,
  accounts,
  pageSize,
}: TransactionsClientProps) {
  const [transactionsPage, setTransactionsPage] = useState(
    initialTransactionsPage,
  );
  const [search, setSearch] = useState("");
  const [accountId, setAccountId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [pendingFilter, setPendingFilter] = useState<PendingFilter>("all");
  const [isLoading, setIsLoading] = useState(false);
  const [updatingTransactionId, setUpdatingTransactionId] = useState<
    string | null
  >(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const isFirstFilterRender = useRef(true);

  const transactions = transactionsPage.items;
  const trimmedSearch = search.trim();
  const hasActiveFilters =
    Boolean(trimmedSearch) ||
    Boolean(accountId) ||
    Boolean(categoryId) ||
    pendingFilter !== "all";

  const transactionsByDate = useMemo(
    () => groupTransactionsByDate(transactions),
    [transactions],
  );

  const loadPage = useCallback(
    async (page: number) => {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const result = await getTransactions({
          search: search.trim() || undefined,
          accountId: accountId || undefined,
          categoryId: categoryId || undefined,
          pending: toPendingQueryValue(pendingFilter),
          page,
          pageSize,
        });

        setTransactionsPage(result);
      } catch (error) {
        setErrorMessage(
          getApiErrorMessage(error, "Could not load transactions."),
        );
      } finally {
        setIsLoading(false);
      }
    },
    [accountId, categoryId, pageSize, pendingFilter, search],
  );

  useEffect(() => {
    if (isFirstFilterRender.current) {
      isFirstFilterRender.current = false;
      return;
    }

    const timeoutId = window.setTimeout(() => {
      void loadPage(1);
    }, 300);

    return () => window.clearTimeout(timeoutId);
  }, [loadPage]);

  function handleClearSearch() {
    setSearch("");
  }

  function handleResetFilters() {
    setSearch("");
    setAccountId("");
    setCategoryId("");
    setPendingFilter("all");
  }

  async function handleCategoryChange(
    transactionId: string,
    nextCategoryId: string,
  ) {
    setUpdatingTransactionId(transactionId);
    setErrorMessage(null);

    try {
      const updatedTransaction = await updateTransactionCategory(
        transactionId,
        {
          categoryId: nextCategoryId || null,
        },
      );

      setTransactionsPage((current) => ({
        ...current,
        items: current.items.map((transaction) =>
          transaction.id === updatedTransaction.id
            ? updatedTransaction
            : transaction,
        ),
      }));
    } catch (error) {
      setErrorMessage(
        getApiErrorMessage(error, "Could not update this transaction."),
      );
    } finally {
      setUpdatingTransactionId(null);
    }
  }

  return (
    <main className="min-h-screen bg-background text-foreground">
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-4 px-6 py-8">
        <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_180px_180px_150px_auto]">
          <div className="relative">
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search"
              aria-busy={isLoading}
              className="app-input h-10 w-full pr-10"
            />

            {search ? (
              <button
                type="button"
                aria-label="Clear search"
                onClick={handleClearSearch}
                className="absolute top-1/2 right-2 -translate-y-1/2 text-muted-foreground transition hover:text-foreground"
              >
                <CircleX className="size-5" />
              </button>
            ) : null}
          </div>

          <select
            value={accountId}
            onChange={(event) => setAccountId(event.target.value)}
            className="app-input h-10 w-full"
            aria-label="Filter by account"
          >
            <option value="">All accounts</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name}
              </option>
            ))}
          </select>

          <select
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            className="app-input h-10 w-full"
            aria-label="Filter by category"
          >
            <option value="">All categories</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>

          <select
            value={pendingFilter}
            onChange={(event) =>
              setPendingFilter(event.target.value as PendingFilter)
            }
            className="app-input h-10 w-full"
            aria-label="Filter by status"
          >
            {STATUS_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>

          {hasActiveFilters ? (
            <button
              type="button"
              onClick={handleResetFilters}
              className="h-10 rounded-md border border-border px-3 text-sm text-muted-foreground transition hover:bg-accent hover:text-foreground"
            >
              Reset
            </button>
          ) : null}
        </div>

        {errorMessage ? (
          <div className="app-panel border-destructive/40 p-4 text-sm text-destructive">
            {errorMessage}
          </div>
        ) : null}

        <div className="app-panel overflow-hidden">
          <div>
            {transactionsByDate.map((group) => (
              <section key={group.date}>
                <div className="border-y border-border/70 bg-muted/20 px-4 py-2.5">
                  <h2 className="text-sm font-semibold text-foreground/90">
                    {formatDateSectionHeader(group.date)}
                  </h2>
                </div>

                <div className="divide-y divide-border/70">
                  {group.transactions.map((transaction) => (
                    <div
                      key={transaction.id}
                      className="grid gap-3 px-4 py-4 text-sm md:grid-cols-[minmax(0,1fr)_190px_120px] md:items-center"
                    >
                      <div className="min-w-0">
                        <p className="truncate font-medium text-foreground">
                          {transaction.name}
                        </p>
                        <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-muted-foreground">
                          <span className="truncate">
                            {transaction.account.name}
                          </span>
                          {transaction.pending ? (
                            <span className="rounded-full bg-muted px-2 py-0.5">
                              Pending
                            </span>
                          ) : null}
                        </div>
                      </div>

                      <select
                        value={transaction.category?.id ?? ""}
                        disabled={updatingTransactionId === transaction.id}
                        onChange={(event) =>
                          void handleCategoryChange(
                            transaction.id,
                            event.target.value,
                          )
                        }
                        className="app-input h-9 w-full text-xs"
                        aria-label={`Set category for ${transaction.name}`}
                      >
                        <option value="">Uncategorized</option>
                        {categories.map((category) => (
                          <option key={category.id} value={category.id}>
                            {category.name}
                          </option>
                        ))}
                      </select>

                      <p
                        className={
                          transaction.amount < 0
                            ? "font-medium tabular-nums text-success md:text-right"
                            : "font-medium tabular-nums text-foreground md:text-right"
                        }
                      >
                        {transaction.amount < 0 ? "+" : "-"}
                        {formatCurrency(transaction.amount)}
                      </p>
                    </div>
                  ))}
                </div>
              </section>
            ))}

            {transactions.length === 0 ? (
              <div className="flex flex-col items-center justify-center gap-3 px-4 py-14 text-center">
                <Search className="size-12 text-muted-foreground/70" />
                <p className="text-base font-semibold text-foreground">
                  No transactions found
                </p>
                <p className="text-sm text-muted-foreground">
                  {trimmedSearch
                    ? `We couldn't find any transactions matching your search of "${trimmedSearch}".`
                    : hasActiveFilters
                      ? "Try adjusting your filters."
                      : "We couldn't find any transactions."}
                </p>
              </div>
            ) : null}
          </div>

          {transactionsPage.totalPages > 1 ? (
            <div className="flex items-center justify-between border-t border-border px-4 py-3 text-sm text-muted-foreground">
              <span>
                Page {transactionsPage.page} of {transactionsPage.totalPages}
              </span>

              <div className="flex gap-2">
                <button
                  type="button"
                  disabled={!transactionsPage.hasPreviousPage || isLoading}
                  onClick={() => void loadPage(transactionsPage.page - 1)}
                  className="cursor-pointer rounded-md border border-border px-3 py-1.5 transition hover:border-primary/50 hover:text-foreground disabled:cursor-not-allowed disabled:opacity-50"
                >
                  Previous
                </button>

                <button
                  type="button"
                  disabled={!transactionsPage.hasNextPage || isLoading}
                  onClick={() => void loadPage(transactionsPage.page + 1)}
                  className="cursor-pointer rounded-md border border-border px-3 py-1.5 transition hover:border-primary/50 hover:text-foreground disabled:cursor-not-allowed disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          ) : null}
        </div>
      </section>
    </main>
  );
}
