"use client";

import { Settings } from "lucide-react";
import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type {
  MerchantHistoryDto,
  MerchantHistoryGranularity,
  TransactionDto,
} from "@/lib/api/types";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";
import {
  periodCurrency,
  type SelectedMerchantPeriod,
} from "./merchantHistoryPeriod";
import { MerchantHistoryChart } from "./MerchantHistoryChart";
import { TransactionRow } from "./TransactionRow";

type MerchantHistoryDrawerProps = {
  open: boolean;
  history: MerchantHistoryDto | null;
  error: string | null;
  isLoading: boolean;
  granularity: MerchantHistoryGranularity;
  selectedPeriodKey: string | null;
  selected: SelectedMerchantPeriod | null;
  onGranularityChange: (granularity: MerchantHistoryGranularity) => void;
  onSelectPeriod: (periodKey: string) => void;
  onClose: () => void;
  onSelectTransaction: (transaction: TransactionDto) => void;
};

const GRANULARITY_OPTIONS: Array<{
  value: MerchantHistoryGranularity;
  label: string;
}> = [
  { value: "monthly", label: "Monthly" },
  { value: "quarterly", label: "Quarterly" },
  { value: "yearly", label: "Yearly" },
];

/**
 * Shows the merchant's history as a chart, a period summary, and the period's transactions.
 * Monthly is the starting range, and closing the chart-range sheet leaves the history open.
 */
export function MerchantHistoryDrawer({
  open,
  history,
  error,
  isLoading,
  granularity,
  selectedPeriodKey,
  selected,
  onGranularityChange,
  onSelectPeriod,
  onClose,
  onSelectTransaction,
}: MerchantHistoryDrawerProps) {
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);

  /**
   * Closes the history sheet.
   * An open chart-range sheet closes first and leaves the history open.
   */
  function handleClose() {
    if (isSettingsOpen) {
      setIsSettingsOpen(false);
      return;
    }

    onClose();
  }

  return (
    <>
      <BottomSheet
        open={open}
        onClose={handleClose}
        title={history?.displayName ?? "History"}
        headerAction="close-leading"
        closeOnEscape={!isSettingsOpen}
        overlayClassName="z-70"
        className={FULL_SCREEN_SHEET_CLASSNAME}
        headerTrailing={
          <Button
            type="button"
            variant="ghost"
            size="icon-lg"
            aria-label="Chart settings"
            onClick={() => setIsSettingsOpen(true)}
          >
            <Settings className="size-5" />
          </Button>
        }
      >
        {error ? <Alert variant="destructive">{error}</Alert> : null}

        {isLoading && !history ? (
          <p className="text-sm text-muted-foreground">Loading history…</p>
        ) : null}

        {history && selectedPeriodKey ? (
          <div className="flex flex-col gap-5">
            <MerchantHistoryChart
              periods={history.periods}
              selectedPeriodKey={selectedPeriodKey}
              onSelectPeriod={onSelectPeriod}
            />

            {selected ? (
              <section className="app-panel p-4">
                <h3 className="text-base font-semibold text-foreground">
                  {selected.label}
                </h3>
                <div className="mt-4 grid gap-3">
                  <SummaryRow
                    label="Total Transactions"
                    value={String(selected.transactionCount)}
                  />
                  <SummaryRow
                    label="Average Transaction"
                    value={formatCurrency(
                      selected.averageAmount,
                      periodCurrency(selected.transactions),
                    )}
                  />
                  <SummaryRow
                    label="Total Amount"
                    value={formatCurrency(
                      selected.totalAmount,
                      periodCurrency(selected.transactions),
                    )}
                  />
                </div>
              </section>
            ) : null}

            <section>
              <h3 className="mb-2 text-sm font-medium text-muted-foreground">
                Transactions
              </h3>
              {selected && selected.transactions.length > 0 ? (
                <div className="divide-y divide-border/70 border-y border-border/70">
                  {selected.transactions.map((transaction) => (
                    <TransactionRow
                      key={transaction.id}
                      transaction={transaction}
                      onSelect={(item) => {
                        onClose();
                        onSelectTransaction(item);
                      }}
                    />
                  ))}
                </div>
              ) : (
                <EmptyState
                  title="No transactions"
                  description="Nothing for this retailer in the selected period."
                  className="py-8 [&_p]:text-sm [&_p]:font-normal"
                />
              )}
            </section>
          </div>
        ) : null}
      </BottomSheet>

      <BottomSheet
        open={isSettingsOpen}
        onClose={() => setIsSettingsOpen(false)}
        title="Chart Range"
        headerAction="close"
        overlayClassName="z-80"
        className="max-h-[50vh]"
      >
        <div className="divide-y divide-border/70 border-y border-border/70">
          {GRANULARITY_OPTIONS.map((option) => {
            const selectedOption = option.value === granularity;

            return (
              <button
                key={option.value}
                type="button"
                onClick={() => {
                  onGranularityChange(option.value);
                  setIsSettingsOpen(false);
                }}
                className="flex min-h-12 w-full items-center justify-between gap-3 py-3 text-left"
              >
                <span className="text-sm font-medium text-foreground">
                  {option.label}
                </span>
                {selectedOption ? (
                  <span className="text-sm text-transfer">Selected</span>
                ) : null}
              </button>
            );
          })}
        </div>
      </BottomSheet>
    </>
  );
}

/**
 * Shows one labeled figure in the period summary.
 */
function SummaryRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-3 text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium text-foreground">{value}</span>
    </div>
  );
}
