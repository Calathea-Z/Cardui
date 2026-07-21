"use client";

import { CircleX } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { SheetSelect } from "@/components/ui/sheet-select";
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
        <Input
          value={query.search}
          onChange={(event) => query.setSearch(event.target.value)}
          placeholder="Search"
          aria-busy={isLoading}
          className="h-10 w-full pr-10"
        />

        {query.search ? (
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label="Clear search"
            onClick={query.clearSearch}
            className="absolute top-1/2 right-2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
          >
            <CircleX className="size-5" />
          </Button>
        ) : null}
      </div>

      <SheetSelect
        title="Account"
        value={query.accountId}
        onChange={query.setAccountId}
        placeholder="All accounts"
        options={[
          { value: "", label: "All accounts" },
          ...accounts.map((account) => ({
            value: account.id,
            label: account.name,
          })),
        ]}
      />

      <SheetSelect
        title="Category"
        value={query.categoryId}
        onChange={query.setCategoryId}
        placeholder="All categories"
        options={[
          { value: "", label: "All categories" },
          ...categories.map((category) => ({
            value: category.id,
            label: category.name,
          })),
        ]}
      />

      <SheetSelect
        title="Status"
        value={query.pendingFilter}
        onChange={(value) => query.setPendingFilter(value as PendingFilter)}
        options={STATUS_OPTIONS.map((option) => ({
          value: option.value,
          label: option.label,
        }))}
      />

      {query.hasActiveFilters ? (
        <Button
          type="button"
          variant="outline"
          onClick={query.resetFilters}
          className="h-10"
        >
          Reset
        </Button>
      ) : null}
    </div>
  );
}
