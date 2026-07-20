"use client";

import { CircleX } from "lucide-react";
import type { AccountDto, CategoryDto } from "@/lib/api/types";
import {
  STATUS_OPTIONS,
  type PendingFilter,
  type TransactionsQueryState,
} from "./useTransactionsQueryState";

type TransactionsFiltersProps = {
  query: TransactionsQueryState;
  accounts: AccountDto[];
  categories: CategoryDto[];
  isLoading: boolean;
};

export function TransactionsFilters({
  query,
  accounts,
  categories,
  isLoading,
}: TransactionsFiltersProps) {
  return (
    <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_180px_180px_150px_auto]">
      <div className="relative">
        <input
          value={query.search}
          onChange={(event) => query.setSearch(event.target.value)}
          placeholder="Search"
          aria-busy={isLoading}
          className="app-input h-10 w-full pr-10"
        />

        {query.search ? (
          <button
            type="button"
            aria-label="Clear search"
            onClick={query.clearSearch}
            className="absolute top-1/2 right-2 -translate-y-1/2 text-muted-foreground transition hover:text-foreground"
          >
            <CircleX className="size-5" />
          </button>
        ) : null}
      </div>

      <select
        value={query.accountId}
        onChange={(event) => query.setAccountId(event.target.value)}
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
        value={query.categoryId}
        onChange={(event) => query.setCategoryId(event.target.value)}
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
        value={query.pendingFilter}
        onChange={(event) =>
          query.setPendingFilter(event.target.value as PendingFilter)
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

      {query.hasActiveFilters ? (
        <button
          type="button"
          onClick={query.resetFilters}
          className="h-10 rounded-md border border-border px-3 text-sm text-muted-foreground transition hover:bg-accent hover:text-foreground"
        >
          Reset
        </button>
      ) : null}
    </div>
  );
}
