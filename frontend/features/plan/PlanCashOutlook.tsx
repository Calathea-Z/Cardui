import Link from "next/link";
import { CircleAlert, TriangleAlert } from "lucide-react";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { SegmentedControl } from "@/components/ui/segmented-control";
import { cn } from "@/lib/utils";
import { PlanCashChart } from "./PlanCashChart";
import {
  planCashRanges,
  rangeLabel,
  type PlanCashRange,
  type PlanCashRangeView,
} from "./planCashRange";
import type { PlanCashNote } from "./planCopy";

type PlanCashOutlookProps = {
  sourceDescription: string;
  hasIncome: boolean;
  typical: PlanCashRangeView;
  lowPay: PlanCashRangeView | null;
  notes: PlanCashNote[];
  currency: string;
  livingSpendingMonthly: number;
  startingCash: number;
  startingReserve: number;
  startingAvailable: number;
  selectedRange: PlanCashRange;
  onRangeChange: (range: PlanCashRange) => void;
};

type CashForecastBodyProps = {
  view: PlanCashRangeView;
  currency: string;
  comparison?: boolean;
};

/**
 * Shows the selected range's supported chart or horizon summary and its three essential figures.
 * A longer range never draws invented intermediate points.
 */
function CashForecastBody({
  view,
  currency,
  comparison = false,
}: CashForecastBodyProps) {
  if (!view.available) {
    return (
      <p className="rounded-md bg-muted/60 px-3 py-3 text-sm text-muted-foreground">
        The current response does not include the selected {view.label} horizon.
        Choose another timeframe or refresh Plan.
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {view.warningText ? (
        <p className="flex items-start gap-2 rounded-md bg-warning/10 px-3 py-2 text-sm font-medium text-warning">
          <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Warning: </span>
            {view.warningText}
          </span>
        </p>
      ) : (
        <p className="text-sm text-foreground">
          Cash stays above zero for the selected {view.label.toLowerCase()}{" "}
          through {view.through}.
        </p>
      )}
      {view.reserveWarningText ? (
        <p className="flex items-start gap-2 text-sm text-warning">
          <CircleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Warning: </span>
            {view.reserveWarningText}
          </span>
        </p>
      ) : null}
      <dl className="grid gap-3 sm:grid-cols-3">
        <div className="rounded-md border border-border p-3">
          <dt className="text-xs text-muted-foreground">Ending cash</dt>
          <dd className="mt-1 text-base font-semibold text-foreground tabular-nums">
            {view.ending}
          </dd>
          <p className="text-xs text-muted-foreground">
            Through {view.through}
          </p>
        </div>
        <div
          className={cn(
            "rounded-md border border-border p-3",
            view.warning && "border-warning/40",
          )}
        >
          <dt className="text-xs text-muted-foreground">Lowest cash</dt>
          <dd
            className={cn(
              "mt-1 text-base font-semibold tabular-nums",
              view.warning ? "text-warning" : "text-foreground",
            )}
          >
            {view.lowest}
          </dd>
          <p className="text-xs text-muted-foreground">On {view.lowestOn}</p>
        </div>
        <div className="rounded-md border border-border p-3">
          <dt className="text-xs text-muted-foreground">
            {view.minimumsLabel}
          </dt>
          <dd className="mt-1 text-sm font-medium text-foreground tabular-nums">
            {view.minimums}
          </dd>
        </div>
      </dl>
      {view.chartRows.length > 0 && view.chartLabel ? (
        <div>
          <h3 className="mb-2 text-sm font-medium text-foreground">
            {comparison ? "Low-pay daily cash" : "Daily cash"}
          </h3>
          <PlanCashChart
            rows={view.chartRows}
            lowest={view.chartLowest}
            currency={currency}
            label={view.chartLabel}
          />
        </div>
      ) : (
        <p className="rounded-md bg-muted/60 px-3 py-2 text-sm text-muted-foreground">
          The current API provides an ending value and a lowest point for this
          horizon, not intermediate daily values, so no line is drawn.
        </p>
      )}
    </div>
  );
}

/**
 * Cash outlook on Plan: one explicit supported range at typical pay, with low pay and source details behind disclosures.
 * Without income it asks for an income source because the forecast would otherwise show cash only draining.
 */
export function PlanCashOutlook({
  sourceDescription,
  hasIncome,
  typical,
  lowPay,
  notes,
  currency,
  livingSpendingMonthly,
  startingCash,
  startingReserve,
  startingAvailable,
  selectedRange,
  onRangeChange,
}: PlanCashOutlookProps) {
  const rangeOptions = planCashRanges.map((range) => ({
    value: range,
    label: rangeLabel(range),
  }));

  return (
    <section className="app-panel" aria-labelledby="plan-cash-title">
      <div className="app-panel-header px-4 py-3">
        <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
          <div>
            <h2 id="plan-cash-title" className="app-section-title">
              Typical pay · {typical.label}
            </h2>
            <p className="app-section-meta">
              {typical.available
                ? `Selected timeframe through ${typical.through}.`
                : "Selected timeframe is unavailable in the current response."}
            </p>
          </div>
          <SegmentedControl
            label="Cash timeframe"
            options={rangeOptions}
            value={selectedRange}
            onChange={onRangeChange}
            className="grid grid-cols-2 md:flex"
          />
        </div>
      </div>
      {!hasIncome ? (
        <EmptyState
          title="Income is required"
          description="Add an income source and this shows how your cash holds up over the next 18 months."
          action={
            <Link
              href="/income"
              className={cn(buttonVariants(), "min-h-11 px-4")}
            >
              Go to Income
            </Link>
          }
        />
      ) : (
        <div className="flex flex-col gap-4 p-4">
          <CashForecastBody view={typical} currency={currency} />
          <details className="rounded-md border border-border">
            <summary className="min-h-11 cursor-pointer rounded-md px-3 py-2.5 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none">
              Compare low pay · {typical.label}
            </summary>
            <div className="border-t border-border p-3">
              {lowPay ? (
                <CashForecastBody
                  view={lowPay}
                  currency={currency}
                  comparison
                />
              ) : (
                <p className="text-sm text-muted-foreground">
                  No income source has a low amount yet. Add one on Income to
                  see this.
                </p>
              )}
            </div>
          </details>
          <details className="rounded-md border border-border">
            <summary className="min-h-11 cursor-pointer rounded-md px-3 py-2.5 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none">
              Sources, protected savings, and omissions
            </summary>
            <div className="flex flex-col gap-3 border-t border-border p-3 text-sm">
              <p className="text-foreground">{sourceDescription}</p>
              <dl className="grid gap-2 sm:grid-cols-3">
                <div>
                  <dt className="text-xs text-muted-foreground">
                    Cash balance
                  </dt>
                  <dd className="font-medium text-foreground tabular-nums">
                    {formatCurrency(startingCash, currency)}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs text-muted-foreground">
                    Protected savings
                  </dt>
                  <dd className="font-medium text-foreground tabular-nums">
                    {formatCurrency(startingReserve, currency)}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs text-muted-foreground">
                    Available after protection
                  </dt>
                  <dd className="font-medium text-foreground tabular-nums">
                    {formatCurrency(startingAvailable, currency)}
                  </dd>
                </div>
              </dl>
              {livingSpendingMonthly > 0 ? (
                <p className="text-muted-foreground">
                  Includes {formatCurrency(livingSpendingMonthly, currency)} a
                  month for flexible spending.{" "}
                  <Link href="/living" className="text-primary underline">
                    Review Plan budget
                  </Link>
                </p>
              ) : null}
              {notes.length > 0 ? (
                <ul className="flex flex-col gap-2">
                  {notes.map((note) => (
                    <li
                      key={note.key}
                      className={cn(
                        "flex items-start gap-2 text-xs",
                        note.warning ? "text-warning" : "text-muted-foreground",
                      )}
                    >
                      {note.warning ? (
                        <CircleAlert
                          aria-hidden
                          className="mt-px size-4 shrink-0"
                        />
                      ) : null}
                      <span>
                        {note.warning ? (
                          <span className="sr-only">Warning: </span>
                        ) : null}
                        {note.text}
                      </span>
                    </li>
                  ))}
                </ul>
              ) : null}
            </div>
          </details>
        </div>
      )}
    </section>
  );
}
