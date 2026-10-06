"use client";

import { formatCurrency } from "@/features/accounts/formatCurrency";
import { cn } from "@/lib/utils";
import type { DebtSummaryReportDto } from "@/lib/api/types";
import { formatSharePercent } from "./debtDisplay";
import {
  summaryInterestNote,
  summaryMetrics,
  summarySignals,
  type SummaryMetric,
} from "./debtSummaryCopy";

type DebtSummaryProps = {
  report: DebtSummaryReportDto | null;
  updating: boolean;
};

/**
 * Totals for the debts on this page.
 * The four figures come first. A two-balance choice stays on the debt. There is no score.
 */
export function DebtSummary({ report, updating }: DebtSummaryProps) {
  const signals = report ? summarySignals(report) : [];

  return (
    <section className="app-panel" aria-labelledby="debt-summary-title">
      <div className="app-panel-header flex items-start justify-between gap-3 px-4 py-3">
        <div className="min-w-0">
          <h2 id="debt-summary-title" className="app-section-title">
            Summary
          </h2>
          <p className="app-section-meta">{summaryInterestNote}</p>
        </div>
        {updating ? (
          <p className="shrink-0 text-xs text-muted-foreground" role="status">
            Updating
          </p>
        ) : null}
      </div>
      {report === null ? (
        <p className="px-4 py-6 text-sm text-muted-foreground">
          This summary could not be loaded. The debts below are still here.
        </p>
      ) : (
        <>
          {report.currencies.map((group) => (
            <div key={group.currency}>
              {report.currencies.length > 1 ? (
                <p className="border-b border-border/70 px-4 py-2 text-xs text-muted-foreground">
                  {group.currency}
                </p>
              ) : null}
              <div className="grid grid-cols-2 divide-x divide-y divide-border/70 sm:grid-cols-4 sm:divide-y-0">
                {summaryMetrics(
                  group,
                  report,
                  formatSharePercent,
                  formatCurrency,
                ).map((item) => (
                  <SummaryFigure key={item.label} metric={item} />
                ))}
              </div>
            </div>
          ))}
          {signals.length > 0 ? (
            <ul className="divide-y divide-border/70 border-t border-border/70">
              {signals.map((signal) => (
                <li
                  key={signal.label}
                  className="flex flex-col gap-0.5 px-4 py-2.5 sm:flex-row sm:items-baseline sm:justify-between sm:gap-4"
                >
                  <span className="text-sm text-foreground">
                    {signal.label}
                  </span>
                  {signal.detail ? (
                    <span className="text-xs text-muted-foreground sm:shrink-0 sm:text-right">
                      {signal.detail}
                    </span>
                  ) : null}
                </li>
              ))}
            </ul>
          ) : null}
        </>
      )}
    </section>
  );
}

type SummaryFigureProps = {
  metric: SummaryMetric;
};

/**
 * One summary figure.
 * The label is quiet, the amount is the point, and an unknown amount stays quiet too.
 */
function SummaryFigure({ metric }: SummaryFigureProps) {
  return (
    <div className="flex min-h-24 min-w-0 flex-col justify-center px-4 py-3">
      <span className="text-xs text-muted-foreground">{metric.label}</span>
      <span
        className={cn(
          "mt-1 truncate text-xl font-semibold tracking-tight tabular-nums",
          metric.known ? "text-foreground" : "text-muted-foreground",
        )}
      >
        {metric.value}
      </span>
      {metric.detail ? (
        <span className="mt-1 text-xs text-muted-foreground">
          {metric.detail}
        </span>
      ) : null}
    </div>
  );
}
