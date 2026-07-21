"use client";

import { useEffect, useState } from "react";
import { getMerchantHistory } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type { TransactionDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { MerchantHistoryDrawer } from "./MerchantHistoryDrawer";

type TransactionHistoryFieldProps = {
  transactionId: string;
  disabled?: boolean;
  onOpenChange?: (open: boolean) => void;
  onSelectTransaction?: (transaction: TransactionDto) => void;
};

function formatHistoryCount(count: number) {
  return count === 1 ? "1 transaction" : `${count} transactions`;
}

export function TransactionHistoryField({
  transactionId,
  disabled = false,
  onOpenChange,
  onSelectTransaction,
}: TransactionHistoryFieldProps) {
  const [totalCount, setTotalCount] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function loadCount() {
      setIsLoading(true);
      setError(null);

      try {
        const result = await getMerchantHistory(transactionId, {
          granularity: "monthly",
        });

        if (!cancelled) {
          setTotalCount(result.totalTransactionCount);
        }
      } catch (err) {
        if (!cancelled) {
          setTotalCount(null);
          setError(getApiErrorMessage(err, "Could not load history."));
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadCount();

    return () => {
      cancelled = true;
    };
  }, [transactionId]);

  function setOpen(open: boolean) {
    setIsOpen(open);
    onOpenChange?.(open);
  }

  const count = totalCount ?? 0;
  const canOpen = !disabled && !isLoading && !error && count > 0;

  return (
    <>
      <button
        type="button"
        disabled={!canOpen}
        onClick={() => setOpen(true)}
        className={cn(
          "flex min-h-12 w-full items-center gap-3 text-left",
          !canOpen && "opacity-50",
        )}
      >
        <span className="shrink-0 text-sm font-medium text-foreground">
          History
        </span>
        <span className="min-w-0 flex-1 truncate text-right text-sm font-medium text-transfer">
          {isLoading
            ? "Loading…"
            : error
              ? "Unavailable"
              : formatHistoryCount(count)}
        </span>
      </button>

      {isOpen ? (
        <MerchantHistoryDrawer
          open={isOpen}
          transactionId={transactionId}
          onClose={() => setOpen(false)}
          onSelectTransaction={(transaction) => {
            setOpen(false);
            onSelectTransaction?.(transaction);
          }}
        />
      ) : null}
    </>
  );
}
