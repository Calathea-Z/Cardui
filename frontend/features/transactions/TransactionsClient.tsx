"use client";

import { useMemo, useState } from "react";
import {
  getTransactions,
  updateTransactionCategory,
  createCategory,
  getApiErrorMessage,
} from "@/lib/api";
import type { CategoryDto, PagedResultDto, TransactionDto } from "@/lib/api";
import { emptyCategoryForm, type CategoryFormState } from "@/lib/categoryForm";

type TransactionsClientProps = {
  initialTransactionsPage: PagedResultDto<TransactionDto>;
  categories: CategoryDto[];
  pageSize: number;
};

function formatCurrency(value: number) {
  const isIncome = value < 0;

  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(isIncome ? Math.abs(value) : value);
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(new Date(value));
}

export function TransactionsClient({
  initialTransactionsPage,
  categories,
  pageSize,
}: TransactionsClientProps) {
  const [transactionsPage, setTransactionsPage] = useState(
    initialTransactionsPage,
  );
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [categoriesState, setCategoriesState] = useState(categories);
  const [categoryForm, setCategoryForm] =
    useState<CategoryFormState>(emptyCategoryForm);
  const [isCreatingCategory, setIsCreatingCategory] = useState(false);
  const [categoryError, setCategoryError] = useState<string | null>(null);

  const transactions = transactionsPage.items;

  const categoryOptions = useMemo(
    () =>
      categoriesState
        .filter((category) => category.key !== "uncategorized")
        .sort((a, b) => a.name.localeCompare(b.name)),
    [categoriesState],
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

  async function handleSearchSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await loadPage(1);
  }

  async function handleCategoryChange(
    transactionId: string,
    categoryId: string,
  ) {
    const nextCategoryId = categoryId === "uncategorized" ? null : categoryId;

    const previousTransactionsPage = transactionsPage;

    setTransactionsPage((currentPage) => ({
      ...currentPage,
      items: currentPage.items.map((transaction) => {
        if (transaction.id !== transactionId) {
          return transaction;
        }

        const nextCategory =
          nextCategoryId === null
            ? null
            : (categoriesState.find(
                (category) => category.id === nextCategoryId,
              ) ?? null);

        return {
          ...transaction,
          category: nextCategory
            ? {
                id: nextCategory.id,
                name: nextCategory.name,
                color: nextCategory.color,
                icon: nextCategory.icon,
              }
            : null,
        };
      }),
    }));

    try {
      const updatedTransaction = await updateTransactionCategory(
        transactionId,
        {
          categoryId: nextCategoryId,
        },
      );

      setTransactionsPage((currentPage) => ({
        ...currentPage,
        items: currentPage.items.map((transaction) =>
          transaction.id === transactionId ? updatedTransaction : transaction,
        ),
      }));
    } catch {
      setTransactionsPage(previousTransactionsPage);
    }
  }

  async function handleCreateCategory(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setCategoryError(null);
    setIsCreatingCategory(true);

    try {
      const createdCategory = await createCategory({
        name: categoryForm.name,
        color: categoryForm.color || null,
        icon: categoryForm.icon || null,
        parentCategoryId: null,
      });

      setCategoriesState((current) => [...current, createdCategory]);
      setCategoryForm(emptyCategoryForm);
    } catch (err) {
      setCategoryError(getApiErrorMessage(err));
    } finally {
      setIsCreatingCategory(false);
    }
  }

  return (
    <main className="min-h-screen bg-background text-foreground">
      <section className="mx-auto flex w-full max-w-6xl flex-col gap-6 px-6 py-8">
        <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-sm text-muted-foreground">Money movement</p>
            <h1 className="text-3xl font-semibold text-violet-50">Transactions</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              {transactionsPage.totalCount.toLocaleString()} total
            </p>
          </div>

          <form
            onSubmit={handleSearchSubmit}
            className="flex w-full gap-2 md:w-auto"
          >
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search transactions"
              className="app-input h-10 w-full md:w-72"
            />

            <button
              type="submit"
              disabled={isLoading}
              className="app-cta-button h-10"
            >
              {isLoading ? "Searching" : "Search"}
            </button>
          </form>
        </div>

        <form
          onSubmit={handleCreateCategory}
          className="app-panel grid gap-3 p-4 md:grid-cols-[1fr_160px_160px_auto]"
        >
          <input
            value={categoryForm.name}
            onChange={(event) =>
              setCategoryForm((current) => ({
                ...current,
                name: event.target.value,
              }))
            }
            placeholder="New category name"
            className="app-input h-10"
          />

          <input
            value={categoryForm.color}
            onChange={(event) =>
              setCategoryForm((current) => ({
                ...current,
                color: event.target.value,
              }))
            }
            placeholder="#22c55e"
            className="app-input h-10"
          />

          <input
            value={categoryForm.icon}
            onChange={(event) =>
              setCategoryForm((current) => ({
                ...current,
                icon: event.target.value,
              }))
            }
            placeholder="Icon"
            className="app-input h-10"
          />

          <button
            type="submit"
            disabled={isCreatingCategory}
            className="app-cta-button h-10"
          >
            {isCreatingCategory ? "Creating" : "New category"}
          </button>

          {categoryError ? (
            <p className="text-sm text-destructive md:col-span-4">
              {categoryError}
            </p>
          ) : null}
        </form>

        <div className="app-panel overflow-hidden">
          <div className="grid grid-cols-[1fr_140px_180px] gap-4 border-b border-border px-4 py-3 text-sm font-medium text-muted-foreground md:grid-cols-[140px_1fr_160px_180px_130px]">
            <span className="hidden md:block">Date</span>
            <span>Description</span>
            <span className="hidden md:block">Account</span>
            <span>Category</span>
            <span className="text-right">Amount</span>
          </div>

          <div className="divide-y divide-border/70">
            {transactions.map((transaction) => (
              <div
                key={transaction.id}
                className="grid grid-cols-[1fr_140px_180px] gap-4 px-4 py-4 text-sm md:grid-cols-[140px_1fr_160px_180px_130px]"
              >
                <div className="hidden text-muted-foreground md:block">
                  {formatDate(transaction.date)}
                </div>

                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <p className="truncate font-medium">{transaction.name}</p>
                    {transaction.pending ? (
                      <span className="rounded-full bg-amber-400/10 px-2 py-0.5 text-xs text-amber-300">
                        Pending
                      </span>
                    ) : null}
                  </div>

                  <p className="mt-1 truncate text-muted-foreground md:hidden">
                    {formatDate(transaction.date)} · {transaction.account.name}
                  </p>

                  {transaction.merchantName ? (
                    <p className="mt-1 truncate text-muted-foreground/80">
                      {transaction.merchantName}
                    </p>
                  ) : null}
                </div>

                <div className="hidden truncate text-muted-foreground md:block">
                  {transaction.account.name}
                </div>

                <select
                  value={transaction.category?.id ?? "uncategorized"}
                  onChange={(event) =>
                    handleCategoryChange(transaction.id, event.target.value)
                  }
                  className="app-input h-9 cursor-pointer px-2"
                >
                  <option value="uncategorized">Uncategorized</option>
                  {categoryOptions.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </select>

                <div
                  className={
                    transaction.amount < 0
                      ? "text-right font-medium text-success"
                      : "text-right font-medium text-foreground"
                  }
                >
                  {transaction.amount < 0 ? "+" : "-"}
                  {formatCurrency(transaction.amount)}
                </div>
              </div>
            ))}

            {transactions.length === 0 ? (
              <div className="px-4 py-12 text-center text-sm text-muted-foreground">
                No transactions found.
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
