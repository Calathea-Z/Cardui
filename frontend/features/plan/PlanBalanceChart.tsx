"use client";

import {
  Area,
  AreaChart,
  CartesianGrid,
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
  formatMonthLabel,
  formatMonthTick,
  monthTicks,
  type PlanBalanceRow,
  type PlanBand,
} from "./planChartSeries";

type PlanBalanceChartProps = {
  bands: PlanBand[];
  rows: PlanBalanceRow[];
  currency: string;
  highlightedDebtId: string | null;
  onHighlight: (debtId: string | null) => void;
};

type BalanceTooltipProps = {
  active?: boolean;
  payload?: Array<{ payload: PlanBalanceRow }>;
  bands: PlanBand[];
  currency: string;
};

/**
 * Tooltip for one month: the total owed, then each debt still open with its swatch, in payoff order.
 * A paid-off debt is left out of that month's list.
 */
function BalanceTooltip({
  active,
  payload,
  bands,
  currency,
}: BalanceTooltipProps) {
  const row = payload?.[0]?.payload;
  if (!active || !row) {
    return null;
  }

  const open = bands.filter((band) => (row[band.key] ?? 0) > 0);
  return (
    <div className="min-w-44 rounded-lg border border-border bg-card px-3 py-2 shadow-lg">
      <p className="text-xs text-muted-foreground">
        {formatMonthLabel(row.timestamp)}
      </p>
      <p className="ledger-amount mt-0.5 text-sm text-foreground">
        {formatChartCurrency(row.total, currency)} owed
      </p>
      {open.length > 0 ? (
        <ul className="mt-2 flex flex-col gap-1 border-t border-border pt-2">
          {open.map((band) => (
            <li
              key={band.key}
              className="flex items-center justify-between gap-4 text-xs"
            >
              <span className="flex min-w-0 items-center gap-1.5 text-foreground">
                <span
                  aria-hidden
                  className="size-2.5 shrink-0 rounded-sm"
                  style={{ backgroundColor: band.color }}
                />
                <span className="truncate">{band.name}</span>
              </span>
              <span className="text-foreground tabular-nums">
                {formatChartCurrency(row[band.key], currency)}
              </span>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

/**
 * Stacked balance chart: one band per debt that pays off, so the top edge is the total owed.
 * Bands stack in payoff order with the first payoff on top, and each thins to nothing at its payoff.
 * Hovering a band, or the matching payoff row, dims the other bands. There is no entrance animation.
 */
export function PlanBalanceChart({
  bands,
  rows,
  currency,
  highlightedDebtId,
  onHighlight,
}: PlanBalanceChartProps) {
  const last = rows[rows.length - 1];
  const label = `Projected balance of each debt that pays off, stacked in payoff order, from ${formatMonthLabel(rows[0].timestamp)} to ${formatMonthLabel(last.timestamp)}.`;

  return (
    <section className="app-panel p-4" aria-labelledby="plan-balance-title">
      <h2 id="plan-balance-title" className="app-section-title">
        Balances
      </h2>
      <p className="app-section-meta">
        The top edge is the total owed. Each band is one debt.
      </p>
      <div className="mt-3 h-[220px] md:h-72" role="img" aria-label={label}>
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart
            data={rows}
            margin={{ top: 8, right: 4, left: 0, bottom: 0 }}
            onMouseLeave={() => onHighlight(null)}
          >
            <CartesianGrid vertical={false} stroke="var(--border)" />
            <XAxis
              type="number"
              dataKey="timestamp"
              domain={[rows[0].timestamp, last.timestamp]}
              ticks={monthTicks(rows)}
              tickFormatter={formatMonthTick}
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
              dx={-4}
            />
            <Tooltip
              content={<BalanceTooltip bands={bands} currency={currency} />}
              cursor={{ stroke: "var(--muted-foreground)", strokeWidth: 1 }}
              isAnimationActive={false}
            />
            {[...bands].reverse().map((band) => (
              <Area
                key={band.key}
                type="monotone"
                dataKey={band.key}
                name={band.name}
                stackId="balance"
                stroke="var(--card)"
                strokeWidth={1}
                fill={band.color}
                fillOpacity={
                  highlightedDebtId === null ||
                  highlightedDebtId === band.debtId
                    ? 0.9
                    : 0.35
                }
                className="[&_path]:transition-[fill-opacity] [&_path]:duration-150 motion-reduce:[&_path]:transition-none"
                dot={false}
                activeDot={false}
                isAnimationActive={false}
                onMouseEnter={() => onHighlight(band.debtId)}
              />
            ))}
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </section>
  );
}
