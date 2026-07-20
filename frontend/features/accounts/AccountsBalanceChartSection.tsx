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
  metric?: AccountChartMetric;
  netWorth?: number;
  compact?: boolean;
  embedded?: boolean;
  showPeriodDelta?: boolean;
  className?: string;
};

export function PeriodDeltaLabel({
  history,
  range,
  metric = DEFAULT_ACCOUNT_CHART_METRIC,
  compact = false,
  className,
}: {
  history: AccountBalanceHistoryPointDto[];
  range: ChartTimeRange;
  metric?: AccountChartMetric;
  compact?: boolean;
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

  const formatted = formatPeriodDelta(periodChange);
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

export function AccountsBalanceChartSection({
  history,
  groups = [],
  metric = DEFAULT_ACCOUNT_CHART_METRIC,
  netWorth = 0,
  compact = false,
  embedded = false,
  showPeriodDelta = false,
  className,
}: AccountsBalanceChartSectionProps) {
  const [range, setRange] = useState<ChartTimeRange>(DEFAULT_CHART_TIME_RANGE);
  const metricOption = getAccountChartMetricOption(metric);
  const total = getMetricTotal(groups, metric, netWorth);

  return (
    <section className={cn("app-panel overflow-hidden", className)}>
      <div className="px-4 pt-4">
        <div className="min-w-0">
          <p className="text-[11px] font-semibold tracking-[0.14em] text-muted-foreground uppercase">
            {metricOption.label}
          </p>
          <p
            className={cn(
              "ledger-amount mt-1 text-foreground",
              compact ? "text-2xl" : "text-3xl",
            )}
          >
            {formatCurrency(
              metric === "net-worth" ? total : Math.abs(total),
            )}
          </p>
          {showPeriodDelta ? (
            <PeriodDeltaLabel
              history={history}
              range={range}
              metric={metric}
              compact={compact}
              className="mt-1.5"
            />
          ) : null}
        </div>
      </div>

      <div className="flex flex-col gap-3 px-4 pt-3 pb-4">
        <AccountsBalanceChart
          history={history}
          range={range}
          metric={metric}
          compact={compact}
          embedded={embedded}
        />
        <ChartTimeRangeSelector
          value={range}
          onChange={setRange}
          compact={compact}
        />
      </div>
    </section>
  );
}
