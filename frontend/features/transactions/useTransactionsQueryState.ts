"use client";

import { useState } from "react";

/**
 * Which transactions the list is limited to.
 * Archived is included with pending and posted.
 */
export type PendingFilter = "all" | "pending" | "posted" | "archived";

export const STATUS_OPTIONS: { value: PendingFilter; label: string }[] = [
  { value: "all", label: "All statuses" },
  { value: "posted", label: "Posted" },
  { value: "pending", label: "Pending" },
  { value: "archived", label: "Archived" },
];

/**
 * Maps the status filter onto the pending query flag.
 * Pending is true, posted is false, and all or archived leave the flag unset.
 */
export function toPendingQueryValue(filter: PendingFilter) {
  if (filter === "pending") {
    return true;
  }

  if (filter === "posted") {
    return false;
  }

  return undefined;
}

/**
 * Holds search, account, category, and status for the transaction list.
 * A filter counts as active when search has text, an account or category is chosen, or the status is pending, posted, or archived.
 */
export function useTransactionsQueryState() {
  const [search, setSearch] = useState("");
  const [accountId, setAccountId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [pendingFilter, setPendingFilter] = useState<PendingFilter>("all");

  const trimmedSearch = search.trim();
  const hasActiveFilters =
    Boolean(trimmedSearch) ||
    Boolean(accountId) ||
    Boolean(categoryId) ||
    pendingFilter !== "all";

  function clearSearch() {
    setSearch("");
  }

  /**
   * Clears search, account, category, and the pending filter.
   */
  function resetFilters() {
    setSearch("");
    setAccountId("");
    setCategoryId("");
    setPendingFilter("all");
  }

  return {
    search,
    setSearch,
    accountId,
    setAccountId,
    categoryId,
    setCategoryId,
    pendingFilter,
    setPendingFilter,
    trimmedSearch,
    hasActiveFilters,
    clearSearch,
    resetFilters,
  };
}

/**
 * The filter values and updaters the transactions screen shares.
 * Includes the trimmed search and whether any filter is active.
 */
export type TransactionsQueryState = ReturnType<
  typeof useTransactionsQueryState
>;
