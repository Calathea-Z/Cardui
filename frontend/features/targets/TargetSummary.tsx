"use client";

import { formatCurrency } from "@/features/accounts/formatCurrency";
import { cn } from "@/lib/utils";
import type { CategoryTargetMonthDto } from "@/lib/api/types";
import {
  spentPeriodNote,
  summaryMetrics,
  summarySignals,
  targetSummaryNote,
} from "./categoryTargetCopy";

type TargetSummaryProps = {
  month: CategoryTargetMonthDto;
  updating: boolean;
};

/**
 * Target, spent, and remaining for the month on screen.
 * Spending with no target is named under the figures and is not folded into remaining.
 */
export function TargetSummary({ month, updating }: TargetSummaryProps) {
  const format = (amount: number) =>
    formatCurrency(amount, month.planningCurrency);
  const signals = summarySignals(month, format);

  return (
    <section className="app-panel" aria-labelledby="target-summary-title">
      <div className="app-panel-header flex items-start justify-between gap-3 px-4 py-3">
        <div className="min-w-0">
          <h2 id="target-summary-title" className="app-section-title">
            Summary
          </h2>
          <p className="app-section-meta">{targetSummaryNote}</p>
          <p className="app-section-meta">{spentPeriodNote(month)}</p>
        </div>
        {updating ? (
          <p className="shrink-0 text-xs text-muted-foreground" role="status">
            Updating
          </p>
        ) : null}
      </div>
      <div className="grid grid-cols-2 divide-x divide-y divide-border/70 sm:grid-cols-3 sm:divide-y-0">
        {summaryMetrics(month, format).map((metric) => (
          <div
            key={metric.label}
            className="flex min-h-24 min-w-0 flex-col justify-center px-4 py-3"
          >
            <span className="text-xs text-muted-foreground">
              {metric.label}
            </span>
            <span
              className={cn(
                "mt-1 truncate text-xl font-semibold tracking-tight tabular-nums",
                metric.known ? "text-foreground" : "text-muted-foreground",
              )}
            >
              {metric.value}
            </span>
          </div>
        ))}
      </div>
      {signals.length > 0 ? (
        <ul className="divide-y divide-border/70 border-t border-border/70">
          {signals.map((signal) => (
            <li key={signal} className="px-4 py-2.5 text-sm text-foreground">
              {signal}
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
