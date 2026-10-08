import { CircleAlert } from "lucide-react";
import { PlanCashChart } from "./PlanCashChart";
import type { PlanCashRangeView } from "./planCashRange";

type PlanCashPreviewProps = {
  view: PlanCashRangeView;
  currency: string;
  startingReserve: number;
};

/**
 * Gives Overview a concise near-term cash picture without repeating the full status warning.
 * The figures remain readable without inspecting the chart.
 */
export function PlanCashPreview({
  view,
  currency,
  startingReserve,
}: PlanCashPreviewProps) {
  return (
    <section className="app-panel p-4" aria-labelledby="plan-preview-title">
      <h2 id="plan-preview-title" className="app-section-title">
        Next 30 days
      </h2>
      <p className="app-section-meta">
        Cash balance at the end of each day. Protected savings remain in that
        balance but reduce what is available.
      </p>
      <dl className="mt-3 grid grid-cols-2 gap-3">
        <div>
          <dt className="text-xs text-muted-foreground">Ending cash</dt>
          <dd className="text-sm font-semibold text-foreground tabular-nums">
            {view.ending}
          </dd>
        </div>
        <div>
          <dt className="text-xs text-muted-foreground">Lowest cash</dt>
          <dd className="text-sm font-semibold text-foreground tabular-nums">
            {view.lowest} on {view.lowestOn}
          </dd>
        </div>
      </dl>
      {view.reserveWarningText ? (
        <p className="mt-3 flex items-start gap-2 text-xs text-warning">
          <CircleAlert aria-hidden className="mt-px size-4 shrink-0" />
          <span>
            Protected savings are under pressure in this 30-day view. The plan
            status above has the date and next action.
          </span>
        </p>
      ) : startingReserve > 0 ? (
        <p className="mt-3 text-xs text-muted-foreground">
          Protected savings are tracked separately from the cash balance.
        </p>
      ) : null}
      {view.chartRows.length > 0 && view.chartLabel ? (
        <div className="mt-4">
          <PlanCashChart
            rows={view.chartRows}
            lowest={view.chartLowest}
            currency={currency}
            label={view.chartLabel}
          />
        </div>
      ) : null}
    </section>
  );
}
