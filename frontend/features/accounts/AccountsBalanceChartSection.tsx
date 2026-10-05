"use client";

import { useMemo, useState } from "react";
import type {
  AccountBalanceHistoryPointDto,
  AccountGroupDto,
} from "@/lib/api/types";
import { cn } from "@/lib/utils";
import {
  DEFAULT_ACCOUNT_CHART_METRIC,
  getAccountChartMetricOption,
  getHistoryValue,
  getMetricTotal,
  type AccountChartMetric,
} from "./accountChartMetric";
import { AccountChartMetricSelector } from "./AccountChartMetricSelector";
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
  groups?: AccountGroupDto[];
  netWorth?: number;
  currency?: string;
  compact?: boolean;
  embedded?: boolean;
  showPeriodDelta?: boolean;
  className?: string;
};

/**
 * Shows how the selected series moved across the visible range.
 * The change uses the chart currency formatter, a decrease is favorable on a liability series, and fewer than two points hides the line.
 */
export function PeriodDeltaLabel({
  history,
  range,
  metric = DEFAULT_ACCOUNT_CHART_METRIC,
  compact = false,
  currency = "USD",
  className,
}: {
  history: AccountBalanceHistoryPointDto[];
  range: ChartTimeRange;
  metric?: AccountChartMetric;
  compact?: boolean;
  currency?: string;
  className?: string;
}) {
  const metricOption = getAccountChartMetricOption(metric);

  const filteredHistory = useMemo(
    () => filterHistoryByRange(history, range),
    [history, range],
  );

  const periodChange = useMemo(
    () =>
      computePeriodChange(filteredHistory, (point) =>
        getHistoryValue(point, metric),
      ),
    [filteredHistory, metric],
  );

  if (!periodChange) {
    return null;
  }

  const formatted = formatPeriodDelta(periodChange, currency);
  const isFavorable = metricOption.isLiability
    ? periodChange.delta <= 0
    : periodChange.delta >= 0;

  return (
    <p
      className={cn(
        "ledger-amount inline-flex items-center gap-1",
        compact ? "text-xs" : "text-sm",
        isFavorable ? "text-success" : "text-destructive",
        className,
      )}
    >
      <span aria-hidden="true" className="text-[0.7em] leading-none">
        {periodChange.delta >= 0 ? "▲" : "▼"}
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

/**
 * Shows the selected series total, its history, and the series and range controls.
 * Net worth keeps its sign, and every other series is shown as a positive amount with formatCurrency.
 * The controls are text. The pressed label is the series or range the chart uses, in the same green as a favorable change.
 */
export function AccountsBalanceChartSection({
  history,
  groups = [],
  netWorth = 0,
  currency = "USD",
  compact = false,
  embedded = false,
  showPeriodDelta = false,
  className,
}: AccountsBalanceChartSectionProps) {
  const [metric, setMetric] = useState<AccountChartMetric>(
    DEFAULT_ACCOUNT_CHART_METRIC,
  );
  const [range, setRange] = useState<ChartTimeRange>(DEFAULT_CHART_TIME_RANGE);
  const metricOption = getAccountChartMetricOption(metric);
  const total = getMetricTotal(groups, metric, netWorth);

  return (
    <section className={cn("app-panel overflow-hidden", className)}>
      <div className="app-panel-header px-4 py-4">
        <p className="text-sm font-semibold text-foreground">
          {metricOption.label}
        </p>
        <p className="ledger-amount mt-2 text-[2rem] text-foreground">
          {formatCurrency(
            metric === "net-worth" ? total : Math.abs(total),
            currency,
          )}
        </p>
        {showPeriodDelta ? (
          <PeriodDeltaLabel
            history={history}
            range={range}
            metric={metric}
            currency={currency}
            compact
            className="mt-1"
          />
        ) : null}
      </div>

      <div className="flex flex-col gap-3 px-4 py-4">
        <AccountsBalanceChart
          history={history}
          range={range}
          metric={metric}
          currency={currency}
          compact={compact}
          embedded={embedded}
        />
        <div className="flex flex-col gap-1">
          <AccountChartMetricSelector value={metric} onChange={setMetric} />
          <ChartTimeRangeSelector value={range} onChange={setRange} />
        </div>
      </div>
    </section>
  );
}
