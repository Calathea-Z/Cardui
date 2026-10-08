"use client";

import {
  Area,
  CartesianGrid,
  ComposedChart,
  Line,
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
  type PlanObligationRow,
} from "./planChartSeries";

type PlanObligationChartProps = {
  rows: PlanObligationRow[];
  currency: string;
};

type ObligationTooltipProps = {
  active?: boolean;
  payload?: Array<{ payload: PlanObligationRow }>;
  currency: string;
};

/**
 * Tooltip for one step: the month, the minimums still due, and the breathing room.
 */
function ObligationTooltip({
  active,
  payload,
  currency,
}: ObligationTooltipProps) {
  const row = payload?.[0]?.payload;
  if (!active || !row) {
    return null;
  }

  return (
    <div className="min-w-44 rounded-lg border border-border bg-card px-3 py-2 shadow-lg">
      <p className="text-xs text-muted-foreground">
        From {formatMonthLabel(row.timestamp)}
      </p>
      <dl className="mt-1 flex flex-col gap-1 text-xs">
        <div className="flex justify-between gap-4">
          <dt className="text-foreground">Monthly minimums</dt>
          <dd className="text-foreground tabular-nums">
            {formatChartCurrency(row.minimums, currency)}
          </dd>
        </div>
        <div className="flex justify-between gap-4">
          <dt className="text-foreground">Breathing room</dt>
          <dd className="text-foreground tabular-nums">
            {formatChartCurrency(row.room, currency)}
          </dd>
        </div>
      </dl>
    </div>
  );
}

/**
 * Step chart of monthly minimums stepping down on each removal date, and breathing room stepping up over a faint fill.
 * Minimums are a dashed gray line and breathing room a solid green one, so the two differ in style as well as color,
 * and both are named in text above the chart.
 */
export function PlanObligationChart({
  rows,
  currency,
}: PlanObligationChartProps) {
  const last = rows[rows.length - 1];
  const label = `Monthly minimums and breathing room from ${formatMonthLabel(rows[0].timestamp)}. Minimums fall from ${formatChartCurrency(rows[0].minimums, currency)} to ${formatChartCurrency(last.minimums, currency)}. Breathing room rises to ${formatChartCurrency(last.room, currency)} a month.`;

  return (
    <section className="app-panel p-4" aria-labelledby="plan-obligation-title">
      <h2 id="plan-obligation-title" className="app-section-title">
        Minimums and breathing room
      </h2>
      <ul className="mt-1 flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
        <li className="flex items-center gap-1.5">
          <span
            aria-hidden
            className="w-4 border-t-2 border-dashed border-muted-foreground"
          />
          Monthly minimums
        </li>
        <li className="flex items-center gap-1.5">
          <span aria-hidden className="h-0.5 w-4 rounded-full bg-success" />
          Breathing room
        </li>
      </ul>
      <div className="mt-3 h-[220px] md:h-60" role="img" aria-label={label}>
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart
            data={rows}
            margin={{ top: 8, right: 4, left: 0, bottom: 0 }}
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
              content={<ObligationTooltip currency={currency} />}
              cursor={{ stroke: "var(--muted-foreground)", strokeWidth: 1 }}
              isAnimationActive={false}
            />
            <Area
              type="stepAfter"
              dataKey="room"
              name="Breathing room"
              stroke="var(--success)"
              strokeWidth={2}
              fill="var(--success)"
              fillOpacity={0.12}
              dot={false}
              activeDot={false}
              isAnimationActive={false}
            />
            <Line
              type="stepAfter"
              dataKey="minimums"
              name="Monthly minimums"
              stroke="var(--muted-foreground)"
              strokeWidth={2}
              strokeDasharray="6 4"
              dot={false}
              activeDot={false}
              isAnimationActive={false}
            />
          </ComposedChart>
        </ResponsiveContainer>
      </div>
    </section>
  );
}
