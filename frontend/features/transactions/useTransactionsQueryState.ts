"use client";

import { useState } from "react";

export type PendingFilter = "all" | "pending" | "posted" | "archived";

export const STATUS_OPTIONS: { value: PendingFilter; label: string }[] = [
  { value: "all", label: "All statuses" },
  { value: "posted", label: "Posted" },
  { value: "pending", label: "Pending" },
  { value: "archived", label: "Archived" },
];

export function toPendingQueryValue(filter: PendingFilter) {
  if (filter === "pending") {
    return true;
  }

  if (filter === "posted") {
    return false;
  }

  return undefined;
}

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

export type TransactionsQueryState = ReturnType<
  typeof useTransactionsQueryState
>;
