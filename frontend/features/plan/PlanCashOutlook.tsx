import Link from "next/link";
import { CircleAlert, TriangleAlert } from "lucide-react";
import { formatCurrency } from "@/features/accounts/formatCurrency";
import { buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { cn } from "@/lib/utils";
import { PlanCashChart } from "./PlanCashChart";
import { PlanCashHorizons } from "./PlanCashHorizons";
import type { PlanCashRow } from "./planChartSeries";
import type {
  PlanCashHorizonCopy,
  PlanCashNote,
  PlanCashSummaryCopy,
} from "./planCopy";

/**
 * Everything one cash forecast shows: the sentence, the 30-day chart, and the horizon cards.
 */
export type PlanCashForecastView = {
  summary: PlanCashSummaryCopy;
  rows: PlanCashRow[];
  lowest: PlanCashRow | null;
  chartLabel: string;
  cards: PlanCashHorizonCopy[];
};

type PlanCashOutlookProps = {
  description: string;
  hasIncome: boolean;
  typical: PlanCashForecastView;
  lowPay: PlanCashForecastView | null;
  notes: PlanCashNote[];
  currency: string;
  livingSpendingMonthly: number;
};

type CashForecastBodyProps = {
  view: PlanCashForecastView;
  currency: string;
};

/**
 * One forecast: the sentence about the next 18 months, the next 30 days, and the 6, 12, and 18 month cards.
 * A shortfall sentence sits in the warning callout with an icon and a spoken "Warning".
 */
function CashForecastBody({ view, currency }: CashForecastBodyProps) {
  return (
    <div className="flex flex-col gap-4">
      {view.summary.warning ? (
        <p className="flex items-start gap-2 rounded-md bg-warning/10 px-3 py-2 text-sm font-medium text-warning">
          <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Warning: </span>
            {view.summary.sentence}
          </span>
        </p>
      ) : (
        <p className="text-sm text-foreground">{view.summary.sentence}</p>
      )}
      {view.summary.reserveSentence ? (
        <p className="flex items-start gap-2 rounded-md bg-warning/10 px-3 py-2 text-sm font-medium text-warning">
          <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Warning: </span>
            {view.summary.reserveSentence}
          </span>
        </p>
      ) : null}
      {view.rows.length > 0 ? (
        <div>
          <h3 className="mb-2 text-xs font-medium text-muted-foreground">
            Next 30 days
          </h3>
          <PlanCashChart
            rows={view.rows}
            lowest={view.lowest}
            currency={currency}
            label={view.chartLabel}
          />
        </div>
      ) : null}
      {view.cards.length > 0 ? (
        <div>
          <h3 className="mb-2 text-xs font-medium text-muted-foreground">
            Further out
          </h3>
          <PlanCashHorizons cards={view.cards} />
        </div>
      ) : null}
    </div>
  );
}

/**
 * Cash outlook on Plan: cash over the next 30 days and at 6, 12, and 18 months, at typical pay, with low pay behind a disclosure.
 * Without income it asks for an income source instead, because the forecast would only show cash draining.
 * Notes say what the forecast leaves out; a warning note gets an icon and a spoken "Warning".
 */
export function PlanCashOutlook({
  description,
  hasIncome,
  typical,
  lowPay,
  notes,
  currency,
  livingSpendingMonthly,
}: PlanCashOutlookProps) {
  return (
    <section className="app-panel" aria-labelledby="plan-cash-title">
      <div className="app-panel-header px-4 py-3">
        <h2 id="plan-cash-title" className="app-section-title">
          Cash outlook
        </h2>
        <p className="app-section-meta">{description}</p>
        {livingSpendingMonthly > 0 ? (
          <p className="mt-2 text-sm text-foreground">
            Plan includes {formatCurrency(livingSpendingMonthly, currency)} a
            month for flexible living spending.{" "}
            <Link href="/living" className="text-primary underline">
              Review living
            </Link>
          </p>
        ) : null}
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
          <details className="rounded-md border border-border">
            <summary className="min-h-11 cursor-pointer rounded-md px-3 py-2.5 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none">
              If pay comes in low
            </summary>
            <div className="border-t border-border p-3">
              {lowPay ? (
                <CashForecastBody view={lowPay} currency={currency} />
              ) : (
                <p className="text-sm text-muted-foreground">
                  No income source has a low amount yet. Add one on Income to
                  see this.
                </p>
              )}
            </div>
          </details>
        </div>
      )}
    </section>
  );
}
