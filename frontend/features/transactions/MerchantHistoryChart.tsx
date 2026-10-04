"use client";

import { useEffect, useRef } from "react";
import type { MerchantHistoryPeriodDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";

type MerchantHistoryChartProps = {
  periods: MerchantHistoryPeriodDto[];
  selectedPeriodKey: string;
  onSelectPeriod: (periodKey: string) => void;
};

/**
 * Draws a bar for each merchant-history period and selects one when tapped.
 * Height follows the absolute total against the tallest period, a zero total draws no bar, and the selected period scrolls into view.
 */
export function MerchantHistoryChart({
  periods,
  selectedPeriodKey,
  onSelectPeriod,
}: MerchantHistoryChartProps) {
  const selectedRef = useRef<HTMLButtonElement>(null);
  const maxAmount = Math.max(
    0,
    ...periods.map((period) => Math.abs(period.totalAmount)),
  );

  useEffect(() => {
    selectedRef.current?.scrollIntoView({
      behavior: "smooth",
      inline: "center",
      block: "nearest",
    });
  }, [selectedPeriodKey, periods]);

  return (
    <div className="overflow-x-auto pb-1">
      <div className="flex min-w-full items-end gap-2 px-1">
        {periods.map((period) => {
          const selected = period.key === selectedPeriodKey;
          const heightPercent =
            maxAmount === 0
              ? 0
              : Math.max(
                  period.totalAmount === 0 ? 0 : 8,
                  (Math.abs(period.totalAmount) / maxAmount) * 100,
                );

          return (
            <button
              key={period.key}
              ref={selected ? selectedRef : undefined}
              type="button"
              onClick={() => onSelectPeriod(period.key)}
              aria-pressed={selected}
              aria-label={`${period.label}, ${period.transactionCount} transactions`}
              className="flex w-12 shrink-0 flex-col items-center gap-2"
            >
              <div className="flex h-36 w-full items-end justify-center rounded-md bg-muted/30 px-1.5 py-1">
                <div
                  className={cn(
                    "w-full rounded-sm transition-colors",
                    selected ? "bg-primary" : "bg-primary/35",
                  )}
                  style={{ height: `${heightPercent}%` }}
                />
              </div>
              <span
                className={cn(
                  "text-[11px] font-medium",
                  selected ? "text-foreground" : "text-muted-foreground",
                )}
              >
                {period.shortLabel}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
