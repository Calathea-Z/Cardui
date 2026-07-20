"use client";

import { useId, useMemo } from "react";
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { AccountBalanceHistoryPointDto } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import {
  DEFAULT_ACCOUNT_CHART_METRIC,
  getAccountChartMetricOption,
  getHistoryValue,
  type AccountChartMetric,
} from "./accountChartMetric";
import {
  type ChartHistoryPoint,
  type ChartTimeRange,
  filterHistoryByRange,
  formatChartAxisCurrency,
  formatChartCurrency,
  formatTooltipDate,
  getChartTimeWindow,
  getDateTickFormatter,
  getRangeTicks,
  toChartHistoryPoints,
} from "./chartTimeRange";

type AccountsBalanceChartProps = {
  history: AccountBalanceHistoryPointDto[];
  range: ChartTimeRange;
  metric?: AccountChartMetric;
  compact?: boolean;
  embedded?: boolean;
};

type ChartTooltipProps = {
  active?: boolean;
  payload?: Array<{ value: number; payload: ChartHistoryPoint }>;
  label?: string | number;
};

function ChartTooltip({ active, payload, label }: ChartTooltipProps) {
  if (!active || !payload?.length || label === undefined || label === null) {
    return null;
  }

  const value = payload[0]?.value;
  const pointDate = payload[0]?.payload.date ?? label;

  if (typeof value !== "number") {
    return null;
  }

  return (
    <div className="rounded-lg border border-border bg-card px-3 py-2 shadow-lg">
      <p className="text-xs text-muted-foreground">
        {formatTooltipDate(pointDate)}
      </p>
      <p className="mt-0.5 text-sm font-semibold tabular-nums text-foreground">
        {formatChartCurrency(value)}
      </p>
    </div>
  );
}

function computeYDomain(
  history: AccountBalanceHistoryPointDto[],
  metric: AccountChartMetric,
) {
  if (history.length === 0) {
    return [0, 0] as [number, number];
  }

  const values = history.map((point) => getHistoryValue(point, metric));
  const min = Math.min(...values);
  const max = Math.max(...values);
  const span = Math.max(max - min, Math.abs(max) * 0.01, 100);
  const padding = span * 0.12;

  return [min - padding, max + padding] as [number, number];
}

export function AccountsBalanceChart({
  history,
  range,
  metric = DEFAULT_ACCOUNT_CHART_METRIC,
  compact = false,
  embedded = false,
}: AccountsBalanceChartProps) {
  const gradientId = useId().replace(/:/g, "");
  const metricOption = getAccountChartMetricOption(metric);

  const filteredHistory = useMemo(
    () => filterHistoryByRange(history, range),
    [history, range],
  );

  const chartPoints = useMemo(
    () => toChartHistoryPoints(filteredHistory),
    [filteredHistory],
  );

  const timeWindow = useMemo(
    () => getChartTimeWindow(history, range),
    [history, range],
  );

  const yDomain = useMemo(
    () => computeYDomain(filteredHistory, metric),
    [filteredHistory, metric],
  );

  const dateTickFormatter = useMemo(
    () => getDateTickFormatter(range),
    [range],
  );

  const xTicks = useMemo(
    () => getRangeTicks(timeWindow, compact ? 4 : 6),
    [timeWindow, compact],
  );

  const hasEnoughData = chartPoints.length >= 2;

  const chartContent = !hasEnoughData ? (
    <div
      className={cn(
        "flex items-center justify-center text-center",
        compact ? "h-40" : "h-72",
      )}
    >
      <p className="max-w-xs text-sm text-muted-foreground">
        Not enough data for this time range.
      </p>
    </div>
  ) : (
    <div
      className={compact ? "h-44" : "h-80"}
      role="img"
      aria-label={`${metricOption.label} balance history chart`}
    >
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart
          data={chartPoints}
          margin={{
            top: 8,
            right: 4,
            left: 0,
            bottom: 0,
          }}
        >
          <defs>
            <linearGradient
              id={`balanceFill-${gradientId}`}
              x1="0"
              y1="0"
              x2="0"
              y2="1"
            >
              <stop offset="0%" stopColor="var(--chart-1)" stopOpacity={0.28} />
              <stop
                offset="55%"
                stopColor="var(--chart-1)"
                stopOpacity={0.1}
              />
              <stop
                offset="100%"
                stopColor="var(--chart-1)"
                stopOpacity={0}
              />
            </linearGradient>
          </defs>

          <CartesianGrid
            vertical={false}
            stroke="var(--muted-foreground)"
            strokeOpacity={0.18}
            strokeDasharray="0"
          />

          <XAxis
            type="number"
            dataKey="timestamp"
            domain={[timeWindow.startMs, timeWindow.endMs]}
            ticks={xTicks}
            tickFormatter={dateTickFormatter}
            tick={{ fill: "var(--muted-foreground)" }}
            fontSize={compact ? 11 : 12}
            tickLine={false}
            axisLine={false}
            dy={8}
          />

          <YAxis
            domain={yDomain}
            tickFormatter={formatChartAxisCurrency}
            tick={{ fill: "var(--muted-foreground)" }}
            fontSize={compact ? 11 : 12}
            tickLine={false}
            axisLine={false}
            width={compact ? 52 : 60}
            tickCount={compact ? 5 : 6}
            dx={-4}
          />

          <Tooltip
            content={<ChartTooltip />}
            cursor={{
              stroke: "var(--chart-1)",
              strokeWidth: 1,
              strokeOpacity: 0.45,
            }}
          />

          <Area
            type="linear"
            dataKey={metricOption.historyKey}
            stroke="var(--chart-1)"
            strokeWidth={2.25}
            fill={`url(#balanceFill-${gradientId})`}
            baseValue={yDomain[0]}
            dot={false}
            activeDot={{
              r: compact ? 4 : 5,
              fill: "var(--chart-1)",
              stroke: "var(--card)",
              strokeWidth: 2,
            }}
            isAnimationActive
            animationDuration={450}
          />        </AreaChart>
      </ResponsiveContainer>
    </div>
  );

  if (embedded) {
    return chartContent;
  }

  return (
    <section className={cn("app-panel p-4", compact && "p-3")}>
      <div className={compact ? "mb-2" : "mb-4"}>
        <h2 className="font-semibold text-foreground">Balance history</h2>
      </div>
      {chartContent}
    </section>
  );
}
