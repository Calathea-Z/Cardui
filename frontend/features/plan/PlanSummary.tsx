import { TriangleAlert } from "lucide-react";
import { cn } from "@/lib/utils";
import type { PlanSummaryCopy } from "./planCopy";

type PlanSummaryProps = {
  summary: PlanSummaryCopy;
};

/**
 * The answer at the top of Plan: one sentence, one big figure, and the quieter figures beside it.
 * The big figure is the only 2rem number on the page.
 * A warning summary gets the warning border, an icon, and a spoken "Warning" so color is not the only signal.
 */
export function PlanSummary({ summary }: PlanSummaryProps) {
  return (
    <section
      className={cn("app-panel p-4", summary.warning && "border-warning/40")}
      aria-labelledby="plan-summary-title"
    >
      <h2 id="plan-summary-title" className="sr-only">
        Summary
      </h2>
      {summary.warning ? (
        <p className="flex items-start gap-2 rounded-md bg-warning/10 px-3 py-2 text-sm font-medium text-warning">
          <TriangleAlert aria-hidden className="mt-0.5 size-4 shrink-0" />
          <span>
            <span className="sr-only">Warning: </span>
            {summary.sentence}
          </span>
        </p>
      ) : (
        <p className="text-sm text-foreground">{summary.sentence}</p>
      )}
      <div className="mt-4 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-xs text-muted-foreground">{summary.figureLabel}</p>
          <p className="text-[2rem] leading-tight font-semibold tracking-tight text-foreground tabular-nums">
            {summary.figure}
          </p>
        </div>
        {summary.details.length > 0 ? (
          <dl className="flex gap-6">
            {summary.details.map((detail) => (
              <div key={detail.label}>
                <dt className="text-xs text-muted-foreground">
                  {detail.label}
                </dt>
                <dd className="text-sm font-medium text-foreground tabular-nums">
                  {detail.value}
                </dd>
              </div>
            ))}
          </dl>
        ) : null}
      </div>
    </section>
  );
}
