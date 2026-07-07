"use client";

import {
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { AccountBalanceHistoryPointDto } from "@/lib/api";
import { cn } from "@/lib/utils";

type AccountsBalanceChartProps = {
  history: AccountBalanceHistoryPointDto[];
  compact?: boolean;
  embedded?: boolean;
};

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(value);
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
  }).format(new Date(value));
}

export function AccountsBalanceChart({
  history,
  compact = false,
  embedded = false,
}: AccountsBalanceChartProps) {
  const chart = (
    <div className={compact ? "h-40" : "h-72"}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={history}>
          <XAxis
            dataKey="date"
            tickFormatter={formatDate}
            stroke="oklch(0.64 0.08 292)"
            fontSize={compact ? 10 : 12}
            tickLine={false}
            axisLine={false}
          />

          <YAxis
            tickFormatter={formatCurrency}
            stroke="oklch(0.64 0.08 292)"
            fontSize={compact ? 10 : 12}
            tickLine={false}
            axisLine={false}
            width={compact ? 64 : 80}
          />

          <Tooltip
            formatter={(value) => formatCurrency(Number(value))}
            labelFormatter={(value) => formatDate(String(value))}
            contentStyle={{
              backgroundColor: "oklch(0.21 0.055 292)",
              border: "1px solid oklch(0.34 0.08 292)",
              borderRadius: "8px",
              color: "oklch(0.94 0.02 292)",
            }}
          />

          <Line
            type="monotone"
            dataKey="netWorth"
            stroke="oklch(0.72 0.19 292)"
            strokeWidth={2}
            dot={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );

  if (embedded) {
    return chart;
  }

  return (
    <section className={cn("app-panel p-4", compact && "p-3")}>
      <div className={compact ? "mb-2" : "mb-4"}>
        <h2 className="font-semibold text-violet-100">Balance history</h2>
      </div>
      {chart}
    </section>
  );
}
