"use client";

import { useId, useState } from "react";
import { CircleX } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import type { AccountDto, CategoryDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { countTransactionChoiceFilters } from "./transactionFilters";
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

type ChoiceFilterChip = {
  id: string;
  label: string;
  onClear: () => void;
};

/**
 * Removes one account, category, or status filter.
 * The label is the current selection.
 */
function ChoiceFilterChipButton({ chip }: { chip: ChoiceFilterChip }) {
  return (
    <button
      type="button"
      onClick={chip.onClear}
      aria-label={`Remove ${chip.label} filter`}
      className="inline-flex h-8 max-w-full items-center gap-1.5 rounded-full border border-border bg-muted px-3 text-xs text-foreground"
    >
      <span className="truncate">{chip.label}</span>
      <CircleX className="size-3.5 shrink-0 text-muted-foreground" />
    </button>
  );
}

/**
 * Lets the household search and filter the transaction list by account, category, and status.
 * Below the large breakpoint, search stays visible and the other controls sit behind Filters. Reset is shown only while a filter is active.
 */
export function TransactionsFilters({
  query,
  accounts,
  categories,
  isLoading,
}: TransactionsFiltersProps) {
  const choiceFiltersId = useId();
  const [choiceFiltersOpen, setChoiceFiltersOpen] = useState(false);
  const choiceFilterCount = countTransactionChoiceFilters({
    accountId: query.accountId,
    categoryId: query.categoryId,
    pendingFilter: query.pendingFilter,
  });
  const choiceFilters = buildChoiceFilterChips(query, accounts, categories);

  return (
    <div className="flex flex-col gap-3">
      <div className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_180px_180px_150px_auto]">
        <div className="flex gap-2 lg:contents">
          <div className="relative min-w-0 flex-1">
            <Input
              value={query.search}
              onChange={(event) => query.setSearch(event.target.value)}
              placeholder="Search"
              aria-label="Search transactions"
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

          <Button
            type="button"
            variant="outline"
            aria-expanded={choiceFiltersOpen}
            aria-controls={choiceFiltersId}
            onClick={() => setChoiceFiltersOpen((open) => !open)}
            className="h-10 shrink-0 lg:hidden"
          >
            {choiceFilterCount > 0
              ? `Filters (${choiceFilterCount})`
              : "Filters"}
          </Button>
        </div>

        <div
          id={choiceFiltersId}
          className={cn(
            "grid gap-3 lg:contents",
            !choiceFiltersOpen && "max-lg:hidden",
          )}
        >
          <Select
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

          <Select
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

          <Select
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
              className="hidden h-10 lg:inline-flex"
            >
              Reset
            </Button>
          ) : null}
        </div>
      </div>

      {choiceFilters.length > 0 ? (
        <div className="flex flex-wrap items-center gap-2 lg:hidden">
          {choiceFilters.map((chip) => (
            <ChoiceFilterChipButton key={chip.id} chip={chip} />
          ))}
          <Button
            type="button"
            variant="ghost"
            onClick={query.resetFilters}
            className="h-8 px-2"
          >
            Reset
          </Button>
        </div>
      ) : null}
    </div>
  );
}

/**
 * Lists the account, category, and status selections that can be removed.
 * An unset choice is left out. Search stays on its own field.
 */
function buildChoiceFilterChips(
  query: TransactionsQueryState,
  accounts: AccountDto[],
  categories: CategoryDto[],
): ChoiceFilterChip[] {
  const chips: ChoiceFilterChip[] = [];

  if (query.accountId) {
    chips.push({
      id: "account",
      label:
        accounts.find((account) => account.id === query.accountId)?.name ??
        "Account",
      onClear: () => query.setAccountId(""),
    });
  }

  if (query.categoryId) {
    chips.push({
      id: "category",
      label:
        categories.find((category) => category.id === query.categoryId)?.name ??
        "Category",
      onClear: () => query.setCategoryId(""),
    });
  }

  if (query.pendingFilter !== "all") {
    chips.push({
      id: "status",
      label:
        STATUS_OPTIONS.find((option) => option.value === query.pendingFilter)
          ?.label ?? "Status",
      onClear: () => query.setPendingFilter("all"),
    });
  }

  return chips;
}
