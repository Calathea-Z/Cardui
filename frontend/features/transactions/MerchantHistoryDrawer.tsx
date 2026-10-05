"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { EmptyState } from "@/components/ui/empty-state";
import { Select } from "@/components/ui/select";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import type {
  MerchantHistoryDto,
  MerchantHistoryGranularity,
  TransactionDto,
} from "@/lib/api/types";
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
 * Monthly is the starting range. The chart range uses Select, and Escape closes that list before the history.
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
  const [rangeOpen, setRangeOpen] = useState(false);

  /**
   * Applies a chart range the list knows.
   * An unknown value is ignored.
   */
  function handleGranularityChange(value: string) {
    const next = GRANULARITY_OPTIONS.find((option) => option.value === value);
    if (next) {
      onGranularityChange(next.value);
    }
  }

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title={history?.displayName ?? "History"}
      headerAction="back"
      closeOnEscape={!rangeOpen}
      presentation="panel"
      overlayClassName="z-[120]"
    >
      <div className="mb-5 border-b border-border/70">
        <Select
          variant="row"
          label="Chart range"
          title="Chart range"
          value={granularity}
          options={GRANULARITY_OPTIONS}
          onChange={handleGranularityChange}
          onOpenChange={setRangeOpen}
          overlayClassName="z-[140]"
        />
      </div>
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
