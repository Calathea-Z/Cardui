"use client";

import { useState } from "react";
import type { TransactionDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { MerchantHistoryDrawer } from "./MerchantHistoryDrawer";
import { useMerchantHistory } from "./useMerchantHistory";

type TransactionHistoryFieldProps = {
  transactionId: string;
  disabled?: boolean;
  onOpenChange?: (open: boolean) => void;
  onSelectTransaction?: (transaction: TransactionDto) => void;
};

/**
 * Writes the merchant history count for the history row.
 * A count of one reads "1 transaction".
 */
function formatHistoryCount(count: number) {
  return count === 1 ? "1 transaction" : `${count} transactions`;
}

/**
 * Shows how many transactions share this merchant and opens that history.
 * The count uses monthly history, and the row stays closed until the count is above zero.
 */
export function TransactionHistoryField({
  transactionId,
  disabled = false,
  onOpenChange,
  onSelectTransaction,
}: TransactionHistoryFieldProps) {
  const history = useMerchantHistory(transactionId);
  const [isOpen, setIsOpen] = useState(false);

  /**
   * Opens or closes the history sheet.
   * Closing returns the chart range to monthly.
   */
  function setOpen(open: boolean) {
    setIsOpen(open);
    if (!open) {
      history.resetGranularity();
    }
    onOpenChange?.(open);
  }

  const count = history.monthlyCount ?? 0;
  const monthlyLoading = history.isLoading && history.granularity === "monthly";
  const monthlyFailed =
    history.error !== null && history.granularity === "monthly";
  const canOpen = !disabled && !monthlyLoading && !monthlyFailed && count > 0;

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
          {monthlyLoading
            ? "Loading…"
            : monthlyFailed
              ? "Unavailable"
              : formatHistoryCount(count)}
        </span>
      </button>

      {isOpen ? (
        <MerchantHistoryDrawer
          open={isOpen}
          history={history.history}
          error={history.error}
          isLoading={history.isLoading}
          granularity={history.granularity}
          selectedPeriodKey={history.selectedPeriodKey}
          selected={history.selected}
          onGranularityChange={history.setGranularity}
          onSelectPeriod={history.setSelectedPeriodKey}
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
