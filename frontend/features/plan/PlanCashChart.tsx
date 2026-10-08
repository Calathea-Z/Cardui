"use client";

import {
  Area,
  ComposedChart,
  CartesianGrid,
  ReferenceDot,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  formatChartAxisCurrency,
  formatChartCurrency,
} from "@/features/accounts/chartTimeRange";
import {
  evenTicks,
  formatDayLabel,
  formatDayTick,
  type PlanCashRow,
} from "./planChartSeries";

type PlanCashChartProps = {
  rows: PlanCashRow[];
  lowest: PlanCashRow | null;
  currency: string;
  label: string;
};

type CashTooltipProps = {
  active?: boolean;
  payload?: Array<{ payload: PlanCashRow }>;
  currency: string;
};

/**
 * Tooltip for one day: ending cash, then only the income, bills, living spending, and debt payments that landed that day.
 * Money in carries a plus and money out a minus, so the sign is in the text and not only in color.
 */
function CashTooltip({ active, payload, currency }: CashTooltipProps) {
  const row = payload?.[0]?.payload;
  if (!active || !row) {
    return null;
  }

  const moves = [
    { label: "Income", amount: row.income, sign: "+" },
    { label: "Bills", amount: row.bills, sign: "−" },
    { label: "Living spending", amount: row.livingSpending, sign: "−" },
    { label: "Debt payments", amount: row.debtPayments, sign: "−" },
  ].filter((move) => move.amount > 0);
  return (
    <div className="min-w-44 rounded-lg border border-border bg-card px-3 py-2 shadow-lg">
      <p className="text-xs text-muted-foreground">
        {formatDayLabel(row.timestamp)}
      </p>
      <p className="ledger-amount mt-0.5 text-sm text-foreground">
        {formatChartCurrency(row.cash, currency)}
      </p>
      {moves.length > 0 ? (
        <dl className="mt-1 flex flex-col gap-1 text-xs">
          {moves.map((move) => (
            <div key={move.label} className="flex justify-between gap-4">
              <dt className="text-muted-foreground">{move.label}</dt>
              <dd className="text-foreground tabular-nums">
                {move.sign}
                {formatChartCurrency(move.amount, currency)}
              </dd>
            </div>
          ))}
        </dl>
      ) : null}
    </div>
  );
}

/**
 * Step chart of cash at the end of each of the next 30 days, with a dashed zero line and a dot on the lowest day.
 * Nothing is written inside the plot, so the tooltip never covers chart text. The key above names each mark.
 */
export function PlanCashChart({
  rows,
  lowest,
  currency,
  label,
}: PlanCashChartProps) {
  const first = rows[0];
  const last = rows[rows.length - 1];

  return (
    <div>
      <ul className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
        <li className="flex items-center gap-1.5">
          <span aria-hidden className="h-0.5 w-4 rounded-full bg-chart-1" />
          Cash at end of day
        </li>
        <li className="flex items-center gap-1.5">
          <span
            aria-hidden
            className="size-2 rounded-full border border-card bg-chart-1 ring-1 ring-chart-1"
          />
          Lowest day
        </li>
        <li className="flex items-center gap-1.5">
          <span
            aria-hidden
            className="w-4 border-t border-dashed border-muted-foreground"
          />
          Zero
        </li>
      </ul>
      <div className="mt-3 h-[220px] md:h-60" role="img" aria-label={label}>
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart
            data={rows}
            margin={{ top: 8, right: 8, left: 0, bottom: 0 }}
          >
            <CartesianGrid vertical={false} stroke="var(--border)" />
            <XAxis
              type="number"
              dataKey="timestamp"
              domain={[first.timestamp, last.timestamp]}
              ticks={evenTicks(rows, 4)}
              tickFormatter={formatDayTick}
              tick={{ fill: "var(--muted-foreground)" }}
              fontSize={12}
              tickLine={false}
              axisLine={false}
              dy={8}
            />
            <YAxis
              tickFormatter={(value) =>
                formatChartAxisCurrency(value, currency)
              }
              tick={{ fill: "var(--muted-foreground)" }}
              fontSize={12}
              tickLine={false}
              axisLine={false}
              width={56}
              tickCount={5}
              domain={[
                (min: number) => Math.min(min, 0),
                (max: number) => Math.max(max, 0),
              ]}
              dx={-4}
            />
            <ReferenceLine
              y={0}
              stroke="var(--muted-foreground)"
              strokeDasharray="4 4"
            />
            <Tooltip
              content={<CashTooltip currency={currency} />}
              cursor={{ stroke: "var(--muted-foreground)", strokeWidth: 1 }}
              isAnimationActive={false}
            />
            <Area
              type="stepAfter"
              dataKey="cash"
              name="Cash at end of day"
              stroke="var(--chart-1)"
              strokeWidth={2}
              fill="var(--chart-1)"
              fillOpacity={0.08}
              dot={false}
              activeDot={{
                r: 4,
                fill: "var(--chart-1)",
                stroke: "var(--card)",
              }}
              isAnimationActive={false}
            />
            {lowest ? (
              <ReferenceDot
                x={lowest.timestamp}
                y={lowest.cash}
                r={5}
                fill="var(--chart-1)"
                stroke="var(--card)"
                strokeWidth={2}
              />
            ) : null}
          </ComposedChart>
        </ResponsiveContainer>
      </div>
      <details className="mt-3 rounded-md border border-border">
        <summary className="min-h-11 cursor-pointer px-3 py-2.5 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none">
          View 30-day values
        </summary>
        <ol className="divide-y divide-border/70 border-t border-border">
          {rows.map((row) => (
            <li key={row.timestamp} className="px-3 py-2.5">
              <div className="flex items-baseline justify-between gap-3">
                <span className="text-sm font-medium text-foreground">
                  {formatDayLabel(row.timestamp)}
                </span>
                <span className="text-sm text-foreground tabular-nums">
                  {formatChartCurrency(row.cash, currency)} ending cash
                </span>
              </div>
              <p className="mt-1 text-xs text-muted-foreground">
                Income +{formatChartCurrency(row.income, currency)} · Bills −
                {formatChartCurrency(row.bills, currency)} · Living −
                {formatChartCurrency(row.livingSpending, currency)} · Debt −
                {formatChartCurrency(row.debtPayments, currency)}
              </p>
            </li>
          ))}
        </ol>
      </details>
    </div>
  );
}
