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

  return {
    transactionsPage,
    transactions: transactionsPage.items,
    isLoading,
    errorMessage,
    loadPage,
  };
}
