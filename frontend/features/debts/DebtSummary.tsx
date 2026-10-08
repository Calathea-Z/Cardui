"use client";

import { CircleAlert } from "lucide-react";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { InfoTip, InfoTipProvider } from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import type { DebtDto, DebtSummaryReportDto } from "@/lib/api/types";
import { formatSharePercent } from "./debtDisplay";
import {
  summaryInterestNote,
  summaryMetrics,
  summarySignals,
  type SummaryDebtName,
  type SummaryMetric,
} from "./debtSummaryCopy";

type DebtSummaryProps = {
  report: DebtSummaryReportDto | null;
  debts: DebtDto[];
  updating: boolean;
};

/**
 * Totals for the debts on this page.
 * The four figures come first. A two-balance choice stays on the debt. There is no score.
 */
export function DebtSummary({ report, debts, updating }: DebtSummaryProps) {
  const names = debtNames(debts);
  const signals = report ? summarySignals(report, names) : [];

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
        <InfoTipProvider>
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
                  names,
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
                  className={cn(
                    "flex flex-col gap-0.5 px-4 py-2.5 sm:flex-row sm:items-baseline sm:justify-between sm:gap-4",
                    signal.warning && "text-warning",
                  )}
                >
                  <span className="flex items-start gap-2 text-sm">
                    {signal.warning ? (
                      <CircleAlert
                        aria-hidden
                        className="mt-0.5 size-4 shrink-0"
                      />
                    ) : null}
                    <span>
                      {signal.warning ? (
                        <span className="sr-only">Warning: </span>
                      ) : null}
                      {signal.label}
                    </span>
                  </span>
                  {signal.detail ? (
                    <Hint
                      text={signal.detail}
                      hint={signal.hint ?? null}
                      className={cn(
                        "text-xs sm:shrink-0 sm:text-right",
                        signal.warning && "text-warning",
                      )}
                    />
                  ) : null}
                </li>
              ))}
            </ul>
          ) : null}
        </InfoTipProvider>
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
        <Hint
          text={metric.detail}
          hint={metric.hint}
          className="mt-1 text-xs"
        />
      ) : null}
    </div>
  );
}

type HintProps = {
  text: string;
  hint: string | null;
  className?: string;
};

/**
 * A caveat that names the debts behind it on hover or focus.
 * A caveat with nothing to name stays plain text.
 */
function Hint({ text, hint, className }: HintProps) {
  if (!hint) {
    return (
      <span className={cn("text-muted-foreground", className)}>{text}</span>
    );
  }

  return (
    <InfoTip
      text={text}
      hint={hint}
      className={cn("text-muted-foreground", className)}
    />
  );
}

/**
 * The names the summary can attach to a gap.
 */
function debtNames(debts: DebtDto[]): SummaryDebtName[] {
  return debts.map((debt) => ({
    id: debt.id,
    name: debt.name,
    currency: debt.currency,
  }));
}
