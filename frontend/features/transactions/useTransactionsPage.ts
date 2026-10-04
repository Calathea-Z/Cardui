"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { getTransactions } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { PagedResultDto, TransactionDto } from "@/lib/api/types";
import {
  toPendingQueryValue,
  type PendingFilter,
} from "./useTransactionsQueryState";

type UseTransactionsPageOptions = {
  initialTransactionsPage: PagedResultDto<TransactionDto>;
  pageSize: number;
  search: string;
  accountId: string;
  categoryId: string;
  pendingFilter: PendingFilter;
};

/**
 * Loads and updates the transaction pages for the current filters.
 * The first render keeps the server page, and a later filter change reloads page 1 after 300 milliseconds.
 */
export function useTransactionsPage({
  initialTransactionsPage,
  pageSize,
  search,
  accountId,
  categoryId,
  pendingFilter,
}: UseTransactionsPageOptions) {
  const [transactionsPage, setTransactionsPage] = useState(
    initialTransactionsPage,
  );
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const isFirstFilterRender = useRef(true);

  /**
   * Fetches one page of transactions for the current filters.
   * Archived loads archived rows and leaves the pending flag unset.
   */
  const loadPage = useCallback(
    async (page: number) => {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const result = await getTransactions({
          search: search.trim() || undefined,
          accountId: accountId || undefined,
          categoryId: categoryId || undefined,
          pending:
            pendingFilter === "archived"
              ? undefined
              : toPendingQueryValue(pendingFilter),
          archived: pendingFilter === "archived" ? true : undefined,
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

  /**
   * Removes one transaction from the loaded page.
   * The total count drops by one and stays at zero once the page is empty.
   */
  const removeTransaction = useCallback((id: string) => {
    setTransactionsPage((page) => ({
      ...page,
      items: page.items.filter((transaction) => transaction.id !== id),
      totalCount: Math.max(0, page.totalCount - 1),
    }));
  }, []);

  /**
   * Replaces one transaction on the loaded page.
   * The page is sorted by date with the newest date first.
   */
  const patchTransaction = useCallback((updated: TransactionDto) => {
    setTransactionsPage((page) => ({
      ...page,
      items: page.items
        .map((transaction) =>
          transaction.id === updated.id ? updated : transaction,
        )
        .sort((left, right) => right.date.localeCompare(left.date)),
    }));
  }, []);

  return {
    transactionsPage,
    transactions: transactionsPage.items,
    isLoading,
    errorMessage,
    loadPage,
    patchTransaction,
    removeTransaction,
  };
}
