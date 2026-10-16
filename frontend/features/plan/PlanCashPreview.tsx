import { CircleAlert, TriangleAlert } from "lucide-react";
import { PlanCashChart } from "./PlanCashChart";
import type { PlanCashRangeView } from "./planCashRange";
import type { SpendableCopy } from "./spendableCopy";

type PlanCashPreviewProps = {
  view: PlanCashRangeView;
  currency: string;
  startingReserve: number;
  spendable: SpendableCopy;
};

/**
 * Shows how much can be used over the next 30 days, then the cash-balance chart.
 * The extra-payment warning is repeated here only when the page status above does not already lead with it.
 */
export function PlanCashPreview({
  view,
  currency,
  startingReserve,
  spendable,
}: PlanCashPreviewProps) {
  return (
    <section className="app-panel p-4" aria-labelledby="plan-preview-title">
      <h2 id="plan-preview-title" className="app-section-title">
        Spendable
      </h2>
      <p className="app-section-meta">{spendable.sentence}</p>
      <p className="mt-3 text-xl font-semibold text-foreground tabular-nums">
        <span className="sr-only">Spendable </span>
        {spendable.figure}
      </p>
      {spendable.shortfall ? (
        <p className="mt-1 text-sm text-foreground">
          Lowest cash left:{" "}
          <span className="font-medium tabular-nums">
            {spendable.shortfall}
          </span>
        </p>
      ) : null}
      {spendable.guardrail && !spendable.leadsStatus ? (
        <p className="mt-3 flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm font-medium text-warning">
          <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Warning: </span>
            {spendable.guardrail}
          </span>
        </p>
      ) : null}
      {spendable.guardrail && spendable.leadsStatus ? (
        <p className="mt-3 flex items-start gap-2 text-xs text-warning">
          <TriangleAlert aria-hidden className="mt-px size-4 shrink-0" />
          <span>
            This extra payment is covered in the status above. The amount here
            is what is left before that extra.
          </span>
        </p>
      ) : null}
      <dl className="mt-4 grid grid-cols-2 gap-3">
        {spendable.pieces.map((piece) => (
          <div key={piece.label}>
            <dt className="text-xs text-muted-foreground">{piece.label}</dt>
            <dd className="text-sm font-semibold text-foreground tabular-nums">
              {piece.value}
            </dd>
          </div>
        ))}
      </dl>
      <p className="mt-3 text-xs text-muted-foreground">
        {spendable.freshness}
      </p>
      {spendable.uncertainties.length > 0 ? (
        <ul className="mt-3 flex flex-col gap-2">
          {spendable.uncertainties.map((line, index) => (
            <li
              key={`${index}-${line}`}
              className="flex items-start gap-2 text-xs text-warning"
            >
              <CircleAlert aria-hidden className="mt-px size-4 shrink-0" />
              <span>{line}</span>
            </li>
          ))}
        </ul>
      ) : null}
      <h3 className="mt-5 text-sm font-semibold text-foreground">
        Cash balance
      </h3>
      <p className="app-section-meta">{spendable.chartNote}</p>
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
