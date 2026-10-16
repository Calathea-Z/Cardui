import { TriangleAlert } from "lucide-react";
import { cn } from "@/lib/utils";
import type { SpendableCopy } from "./spendableCopy";

type PlanSafetyProps = {
  spendable: SpendableCopy;
};

/**
 * The next-30-day safety answer.
 * A shortfall leads with its amount. The lowest-balance date stays labeled as that low point.
 * Missing facts stay a warning. A covered window does not use a success mark.
 */
export function PlanSafety({ spendable }: PlanSafetyProps) {
  const warning =
    spendable.status === "short" || spendable.status === "incomplete";
  const otherLimits = spendable.uncertainties.slice(1);

  return (
    <section
      id="plan-safety"
      className={cn(
        "app-panel scroll-mt-[calc(3.75rem+env(safe-area-inset-top))] p-4",
        warning && "border-warning/40",
      )}
      aria-labelledby="plan-safety-title"
    >
      <h2 id="plan-safety-title" className="app-section-title">
        Next 30 days
      </h2>
      {warning ? (
        <p className="mt-3 flex items-start gap-2 rounded-md bg-warning/10 px-3 py-2 text-sm font-medium text-warning">
          <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Warning: </span>
            {spendable.sentence}
          </span>
        </p>
      ) : (
        <p className="app-section-meta">{spendable.sentence}</p>
      )}
      <p className="mt-4 text-xs text-muted-foreground">
        {spendable.figureLabel}
      </p>
      <p className="text-[2rem] leading-tight font-semibold tracking-tight text-foreground tabular-nums">
        {spendable.figure}
      </p>
      <p className="mt-2 text-sm text-foreground">{spendable.basis}</p>
      <p className="mt-1 text-sm text-muted-foreground">
        Lowest balance:{" "}
        <span className="font-medium text-foreground tabular-nums">
          {spendable.lowestBalance}
        </span>
      </p>
      <p className="mt-1 text-sm text-muted-foreground">
        {spendable.protectedMoney}
      </p>
      <p className="mt-2 text-sm text-foreground">{spendable.beforeExtra}</p>
      {spendable.guardrail ? (
        <p className="mt-3 flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-sm font-medium text-warning">
          <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Warning: </span>
            {spendable.guardrail} {spendable.beforeExtra}
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
      <p className="mt-2 text-xs text-muted-foreground">
        {spendable.chartNote}
      </p>
      {spendable.uncertainties[0] ? (
        <p className="mt-3 flex items-start gap-2 text-sm text-warning">
          <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>{spendable.uncertainties[0]}</span>
        </p>
      ) : null}
      {otherLimits.length > 0 ? (
        <details className="mt-2">
          <summary className="min-h-11 cursor-pointer py-2 text-sm font-medium text-foreground focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none">
            {otherLimits.length} other{" "}
            {otherLimits.length === 1 ? "limit" : "limits"}
          </summary>
          <ul className="flex flex-col gap-2 pb-2">
            {otherLimits.map((line) => (
              <li key={line} className="text-sm text-muted-foreground">
                {line}
              </li>
            ))}
          </ul>
        </details>
      ) : null}
    </section>
  );
}
