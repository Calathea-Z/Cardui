"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { CircleX, Search } from "lucide-react";
import { getTransactions } from "@/lib/api";
import type { PagedResultDto, TransactionDto } from "@/lib/api";

type TransactionsClientProps = {
  initialTransactionsPage: PagedResultDto<TransactionDto>;
  pageSize: number;
};

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
    ...(date.getFullYear() !== new Date().getFullYear() ? { year: "numeric" } : {}),
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

export function TransactionsClient({
  initialTransactionsPage,
  pageSize,
}: TransactionsClientProps) {
  const [transactionsPage, setTransactionsPage] = useState(
    initialTransactionsPage,
  );
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const isFirstSearchRender = useRef(true);
  const skipNextSearchEffect = useRef(false);

  const transactions = transactionsPage.items;
  const trimmedSearch = search.trim();

  const transactionsByDate = useMemo(
    () => groupTransactionsByDate(transactions),
    [transactions],
  );

  async function loadPage(page: number, searchTerm = search.trim()) {
    setIsLoading(true);

    try {
      const result = await getTransactions({
        search: searchTerm || undefined,
        page,
        pageSize,
      });

      setTransactionsPage(result);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    if (isFirstSearchRender.current) {
      isFirstSearchRender.current = false;
      return;
    }

    if (skipNextSearchEffect.current) {
      skipNextSearchEffect.current = false;
      return;
    }

    const timeoutId = window.setTimeout(() => {
      void loadPage(1);
    }, 300);

    return () => window.clearTimeout(timeoutId);
  }, [search]);

  function handleClearSearch() {
    skipNextSearchEffect.current = true;
    setSearch("");
    setTransactionsPage(initialTransactionsPage);
  }

  return (
    <main className="min-h-screen bg-background text-foreground">
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-4 px-6 py-8">
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
              className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground transition hover:text-foreground"
            >
              <CircleX className="size-5" />
            </button>
          ) : null}
        </div>

        <div className="app-panel overflow-hidden">
          <div>
            {transactionsByDate.map((group) => (
              <section key={group.date}>
                <div className="border-y border-border/70 bg-muted/20 px-4 py-2.5">
                  <h2 className="text-sm font-semibold text-violet-200">
                    {formatDateSectionHeader(group.date)}
                  </h2>
                </div>

                <div className="divide-y divide-border/70">
                  {group.transactions.map((transaction) => (
                    <div
                      key={transaction.id}
                      className="flex items-center justify-between gap-4 px-4 py-4 text-sm"
                    >
                      <p className="min-w-0 truncate font-medium">
                        {transaction.name}
                      </p>

                      <p
                        className={
                          transaction.amount < 0
                            ? "shrink-0 font-medium tabular-nums text-success"
                            : "shrink-0 font-medium tabular-nums text-foreground"
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
                  onClick={() => loadPage(transactionsPage.page - 1)}
                  className="cursor-pointer rounded-md border border-border px-3 py-1.5 transition hover:border-primary/50 hover:text-foreground disabled:cursor-not-allowed disabled:opacity-50"
                >
                  Previous
                </button>

                <button
                  type="button"
                  disabled={!transactionsPage.hasNextPage || isLoading}
                  onClick={() => loadPage(transactionsPage.page + 1)}
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
