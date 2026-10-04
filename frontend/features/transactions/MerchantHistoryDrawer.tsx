"use client";

import { Settings } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Alert } from "@/components/ui/alert";
import { BottomSheet } from "@/components/ui/bottom-sheet";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { getMerchantHistory } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type {
  MerchantHistoryDto,
  MerchantHistoryGranularity,
  TransactionDto,
} from "@/lib/api/types";
import { FULL_SCREEN_SHEET_CLASSNAME } from "./fullScreenSheet";
import { getSelectedMerchantPeriod } from "./merchantHistoryPeriod";
import { MerchantHistoryChart } from "./MerchantHistoryChart";
import { TransactionRow } from "./TransactionRow";

type MerchantHistoryDrawerProps = {
  open: boolean;
  transactionId: string;
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
 * Chooses the currency for a period's totals.
 * The first row's code is used when every row maps to the same code, and mixed codes use USD.
 */
function periodCurrency(transactions: TransactionDto[]) {
  const codes = new Set(
    transactions.map((transaction) => transaction.isoCurrencyCode ?? "USD"),
  );

  if (codes.size === 1) {
    return transactions[0]?.isoCurrencyCode;
  }

  return "USD";
}

/**
 * Shows the merchant's history as a chart, a period summary, and the period's transactions.
 * Monthly is the starting range, and closing the chart-range sheet leaves the history open.
 */
export function MerchantHistoryDrawer({
  open,
  transactionId,
  onClose,
  onSelectTransaction,
}: MerchantHistoryDrawerProps) {
  const [granularity, setGranularity] =
    useState<MerchantHistoryGranularity>("monthly");
  const [selectedPeriodKey, setSelectedPeriodKey] = useState<string | null>(
    null,
  );
  const [history, setHistory] = useState<MerchantHistoryDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);

  useEffect(() => {
    if (!open) {
      return;
    }

    let cancelled = false;

    /**
     * Loads merchant history for the open sheet and the chosen range.
     * Closing the sheet, or changing the transaction or range, ignores the result.
     */
    async function loadHistory() {
      setIsLoading(true);
      setError(null);

      try {
        const result = await getMerchantHistory(transactionId, {
          granularity,
        });

        if (cancelled) {
          return;
        }

        setHistory(result);
        setSelectedPeriodKey(result.selectedPeriodKey);
      } catch (err) {
        if (!cancelled) {
          setHistory(null);
          setError(getApiErrorMessage(err, "Could not load merchant history."));
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadHistory();

    return () => {
      cancelled = true;
    };
  }, [open, transactionId, granularity]);

  const selected = useMemo(() => {
    if (!history || !selectedPeriodKey) {
      return null;
    }

    return getSelectedMerchantPeriod(
      history.periods,
      selectedPeriodKey,
      history.transactions,
      history.granularity,
    );
  }, [history, selectedPeriodKey]);

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
              onSelectPeriod={setSelectedPeriodKey}
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
                  setGranularity(option.value);
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
