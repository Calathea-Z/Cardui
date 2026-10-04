"use client";

import { useEffect, useMemo, useState } from "react";
import { getMerchantHistory } from "@/lib/api/browser";
import { getApiErrorMessage } from "@/lib/api/errors";
import type {
  MerchantHistoryDto,
  MerchantHistoryGranularity,
} from "@/lib/api/types";
import { getSelectedMerchantPeriod } from "./merchantHistoryPeriod";

/**
 * Loads one merchant's history and keeps the selected chart period.
 * The history row's count stays on the monthly total when the chart range changes.
 */
export function useMerchantHistory(transactionId: string) {
  const [granularity, setGranularity] =
    useState<MerchantHistoryGranularity>("monthly");
  const [selectedPeriodKey, setSelectedPeriodKey] = useState<string | null>(
    null,
  );
  const [history, setHistory] = useState<MerchantHistoryDto | null>(null);
  const [monthlyCount, setMonthlyCount] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    /**
     * Loads merchant history for this transaction and chart range.
     * A newer transaction or range drops the result from an older request.
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
        if (granularity === "monthly") {
          setMonthlyCount(result.totalTransactionCount);
        }
      } catch (err) {
        if (cancelled) {
          return;
        }

        setHistory(null);
        setError(getApiErrorMessage(err, "Could not load merchant history."));
        if (granularity === "monthly") {
          setMonthlyCount(null);
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
  }, [transactionId, granularity]);

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
   * Returns the chart to monthly history.
   * Closing the history sheet uses this so the next open starts on monthly.
   */
  function resetGranularity() {
    setGranularity("monthly");
  }

  return {
    granularity,
    setGranularity,
    selectedPeriodKey,
    setSelectedPeriodKey,
    history,
    monthlyCount,
    error,
    isLoading,
    selected,
    resetGranularity,
  };
}
