"use client";

import { useState } from "react";
import { Cell, Pie, PieChart, ResponsiveContainer } from "recharts";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { cn } from "@/lib/utils";
import type { PlanOwedShare } from "./planChartSeries";

type PlanOwedChartProps = {
  shares: PlanOwedShare[];
  currency: string;
  missingBalanceNames: string[];
  embedded?: boolean;
};

/**
 * What you owe today: a donut with one slice per debt, sized by balance, and the total in the middle.
 * The legend names each debt with its balance and percent, so color is not the only label.
 * Hovering a slice or a legend row dims the other slices and puts that debt in the middle instead of a floating tooltip,
 * which would cover the total. Debts with no balance are named below, not drawn.
 */
export function PlanOwedChart({
  shares,
  currency,
  missingBalanceNames,
  embedded = false,
}: PlanOwedChartProps) {
  const [activeDebtId, setActiveDebtId] = useState<string | null>(null);
  const total = shares.reduce((sum, share) => sum + share.balance, 0);
  const active = shares.find((share) => share.debtId === activeDebtId);
  const label = `What you owe today, ${formatCurrency(total, currency)} in total: ${shares
    .map(
      (share) =>
        `${share.name} ${share.percent}%, ${formatCurrency(share.balance, currency)}`,
    )
    .join("; ")}.`;

  return (
    <section
      className={cn("p-4", !embedded && "app-panel")}
      aria-labelledby="plan-owed-title"
    >
      <h2 id="plan-owed-title" className="app-section-title">
        What you owe today
      </h2>
      <p className="app-section-meta">Each debt&apos;s share of the total.</p>
      <div className="mt-4 flex flex-col items-center gap-6 sm:flex-row sm:items-center">
        <div
          className="relative size-48 shrink-0"
          role="img"
          aria-label={label}
          onMouseLeave={() => setActiveDebtId(null)}
        >
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={shares}
                dataKey="balance"
                nameKey="name"
                innerRadius="62%"
                outerRadius="100%"
                paddingAngle={shares.length > 1 ? 1.5 : 0}
                stroke="var(--card)"
                strokeWidth={2}
                startAngle={90}
                endAngle={-270}
                isAnimationActive={false}
                onMouseEnter={(_, index) =>
                  setActiveDebtId(shares[index]?.debtId ?? null)
                }
              >
                {shares.map((share) => (
                  <Cell
                    key={share.debtId}
                    fill={share.color}
                    fillOpacity={
                      activeDebtId === null || activeDebtId === share.debtId
                        ? 0.95
                        : 0.35
                    }
                    className="transition-[fill-opacity] duration-150 motion-reduce:transition-none"
                  />
                ))}
              </Pie>
            </PieChart>
          </ResponsiveContainer>
          <div
            aria-hidden
            className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center text-center"
          >
            <span className="max-w-26 truncate text-xs text-muted-foreground">
              {active ? active.name : "Total"}
            </span>
            <span className="text-base font-semibold text-foreground tabular-nums">
              {formatCurrency(active ? active.balance : total, currency)}
            </span>
            {active ? (
              <span className="text-xs font-medium text-foreground tabular-nums">
                {active.percent}%
              </span>
            ) : null}
          </div>
        </div>
        <ul className="flex w-full min-w-0 flex-col gap-1">
          {shares.map((share) => (
            <li
              key={share.debtId}
              onMouseEnter={() => setActiveDebtId(share.debtId)}
              onMouseLeave={() => setActiveDebtId(null)}
              className={cn(
                "flex items-center gap-3 rounded-md px-2 py-2 transition-opacity duration-150 motion-reduce:transition-none",
                activeDebtId !== null &&
                  activeDebtId !== share.debtId &&
                  "opacity-50",
              )}
            >
              <span
                aria-hidden
                className="size-3 shrink-0 rounded-sm"
                style={{ backgroundColor: share.color }}
              />
              <span className="min-w-0 flex-1 truncate text-sm text-foreground">
                {share.name}
              </span>
              <span className="shrink-0 text-sm text-foreground tabular-nums">
                {formatCurrency(share.balance, currency)}
              </span>
              <span className="w-10 shrink-0 text-right text-sm font-medium text-foreground tabular-nums">
                {share.percent}%
              </span>
            </li>
          ))}
        </ul>
      </div>
      {missingBalanceNames.length > 0 ? (
        <p className="mt-3 text-xs text-muted-foreground">
          Not counted, no balance: {missingBalanceNames.join(", ")}.
        </p>
      ) : null}
    </section>
  );
}
