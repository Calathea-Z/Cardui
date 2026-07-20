"use client";

import { useMemo, useState } from "react";
import type { AccountBalanceHistoryPointDto } from "@/lib/api";
import { cn } from "@/lib/utils";
import { AccountsBalanceChart } from "./AccountsBalanceChart";
import { ChartTimeRangeSelector } from "./ChartTimeRangeSelector";
import {
  DEFAULT_CHART_TIME_RANGE,
  type ChartTimeRange,
  computePeriodChange,
  filterHistoryByRange,
  formatPeriodDelta,
  getRangeLabel,
} from "./chartTimeRange";
import { formatCurrency } from "./formatCurrency";

type AccountsBalanceChartSectionProps = {
  history: AccountBalanceHistoryPointDto[];
  netWorth?: number;
  compact?: boolean;
  embedded?: boolean;
  showPeriodDelta?: boolean;
  className?: string;
};

export function PeriodDeltaLabel({
  history,
  range,
  compact = false,
  className,
}: {
  history: AccountBalanceHistoryPointDto[];
  range: ChartTimeRange;
  compact?: boolean;
  className?: string;
}) {
  const filteredHistory = useMemo(
    () => filterHistoryByRange(history, range),
    [history, range],
  );

  const periodChange = useMemo(
    () => computePeriodChange(filteredHistory),
    [filteredHistory],
  );

  if (!periodChange) {
    return null;
  }

  const formatted = formatPeriodDelta(periodChange);
  const isPositive = periodChange.delta >= 0;

  return (
    <p
      className={cn(
        "inline-flex items-center gap-1 tabular-nums",
        compact ? "text-xs" : "text-sm",
        isPositive ? "text-success" : "text-destructive",
        className,
      )}
    >
      <span aria-hidden="true" className="text-[0.7em] leading-none">
        {isPositive ? "▲" : "▼"}
      </span>
      <span>
        {formatted.amount}
        {formatted.percent ? ` (${formatted.percent})` : ""}{" "}
        <span className="text-muted-foreground">
          {getRangeLabel(range)} change
        </span>
      </span>
    </p>
  );
}

export function AccountsBalanceChartSection({
  history,
  netWorth,
  compact = false,
  embedded = false,
  showPeriodDelta = false,
  className,
}: AccountsBalanceChartSectionProps) {
  const [range, setRange] = useState<ChartTimeRange>(DEFAULT_CHART_TIME_RANGE);
  const showSummaryHeader = typeof netWorth === "number";

  return (
    <section className={cn("app-panel overflow-hidden", className)}>
      {showSummaryHeader ? (
        <div className="flex flex-col gap-4 px-4 pt-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="min-w-0">
            <p className="text-[11px] font-semibold tracking-[0.08em] text-muted-foreground uppercase">
              Net worth
            </p>
            <p
              className={cn(
                "mt-1 font-semibold tabular-nums text-foreground",
                compact ? "text-2xl" : "text-3xl",
              )}
            >
              {formatCurrency(netWorth)}
            </p>
            {showPeriodDelta ? (
              <PeriodDeltaLabel
                history={history}
                range={range}
                compact={compact}
                className="mt-1.5"
              />
            ) : null}
          </div>

          <ChartTimeRangeSelector
            value={range}
            onChange={setRange}
            compact
            className="sm:justify-end"
          />
        </div>
      ) : showPeriodDelta ? (
        <div className="px-4 pt-4">
          <PeriodDeltaLabel history={history} range={range} compact={compact} />
        </div>
      ) : null}

      <div className={cn("px-4", showSummaryHeader ? "pt-3 pb-4" : "py-4")}>
        <AccountsBalanceChart
          history={history}
          range={range}
          compact={compact}
          embedded={embedded || showSummaryHeader}
        />

        {!showSummaryHeader ? (
          <ChartTimeRangeSelector
            value={range}
            onChange={setRange}
            compact={compact}
            className="mt-3"
          />
        ) : null}
      </div>
    </section>
  );
}
